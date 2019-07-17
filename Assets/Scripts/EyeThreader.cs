using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Threading;
using System.IO;
using ViveSR.anipal.Eye;
using System.Collections.Concurrent;
using static BlinkTracker;
using static EyeTracker;
using LSL;

public class EyeThreader : MonoBehaviour
{
    public EyeData data = new EyeData();
    private Thread thread;

    private const int FrequencyControl = 1;
    private const int MaxFrameCount = 3600;


    public ConcurrentQueue<BlinkData> blink_data = new ConcurrentQueue<BlinkData>();
    public ConcurrentQueue<EyeInfoData> gaze_data = new ConcurrentQueue<EyeInfoData>();

    public const int MAX_FRAME_COUNT = 7200;
    public const float THRESHOLD_CLOSE_EYE_VALUE = 0.3f;

    public float m_MinimumBlinkFrames = 6; // The minimum number of frames necessary to record a blink.
    public float m_SmallBlinkFrames = 36; // Default is 0.3 seconds.
    public float m_MediumBlinkFrames = 84; // Default is 0.7 seconds -- After that its all Extended blinks.

    public const int m_GAZE_FRAME_WINDOW = 5;

    [HideInInspector] public volatile int frameCounter;

    // LabStreamingLayer Integration

    private const string unique_source_id = "A8794C21-8607-49B1-9754-44F9FA144F63";

    public string lslStreamName = "Unity_EyeGazeAndPupil_120Hz";
    public string lslStreamType = "Eye_Gaze_Data";

    private liblsl.StreamInfo lslStreamInfo;
    private liblsl.StreamOutlet lslOutlet;
    private const int lslChannelCount = 7;

    private double nominal_srate = 120;
    private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_float32;


    void Start()
    {
        frameCounter = 0;
        //thread = new Thread(QueryEyeData);
        thread = new Thread(EyeDataProcess);
        thread.Start();
    }

    private void OnApplicationQuit()
    {
        thread.Abort();
    }

    private void OnDisable()
    {
        thread.Abort();
    }

    // You can only use C# native function in Unity's thread.
    // Use EyeData's frame_sequence to calculate frame numbers and record data in file.
    void QueryEyeData()
    {
        int FrameCount = 0;
        int PrevFrameSequence = 0, CurrFrameSequence = 0;
        bool StartRecord = false;

        while (FrameCount < MaxFrameCount)
        {
            ViveSR.Error error = SRanipal_Eye.GetEyeData(ref data);
            if (error == ViveSR.Error.WORK)
            {
                CurrFrameSequence = data.frame_sequence;
                if (CurrFrameSequence != PrevFrameSequence)
                {
                    FrameCount++;
                    PrevFrameSequence = CurrFrameSequence;
                    StartRecord = true;
                }
            }

            // Record time stamp every 120 frame.
            if (FrameCount % 120 == 0 && StartRecord)
            {
                long ms = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                string text = "CurrentFrameSequence: " + CurrFrameSequence +
                    " CurrentSystemTime(ms): " + ms.ToString() + Environment.NewLine;
                File.AppendAllText("DataRecord.txt", text);
                FrameCount = 0;
            }
            Thread.Sleep(FrequencyControl);
        }
    }

    void EyeDataProcess()
    {
        /*
         * Eye Blinking Data Structures, which are being passed to TreatBlinkData as References!  
         */
        // Left Eye Info
        List<float> leftBlinkTracker = new List<float>();
        int startTimeStamp_Left = 0;

        // Right Eye Info
        List<float> rightBlinkTracker = new List<float>();
        int startTimeStamp_Right = 0;

        // Auxilliary Variables
        bool MAXCOUNTER_REACHED_LEFT = false;
        bool MAXCOUNTER_REACHED_RIGHT = false;

        /*
         * Gaze Data Structures, which are being passed to GazeDataRay as References!
         */
        List<Ray> gazeDataWindow = new List<Ray>();


        lslOutlet = KickStartLSLStream(); // Start the LSL Streamer
        float[ , ] gazeSampler = new float[100, lslChannelCount];
        int sampleCounter = 0;

        while (true)
        {
            ViveSR.Error error = SRanipal_Eye.GetEyeData(ref data);

            if(error == ViveSR.Error.WORK)
            {
                // Eye Blinking Data Treatment.
                float open_left = data.verbose_data.left.eye_openness;
                float open_right = data.verbose_data.right.eye_openness;
                TreatBlinkData(open_left, open_right, ref leftBlinkTracker, ref rightBlinkTracker, ref startTimeStamp_Left, ref startTimeStamp_Right, ref MAXCOUNTER_REACHED_LEFT, ref MAXCOUNTER_REACHED_RIGHT);

                // Gaze Data Handling with LabStreaming Layer
                Vector3 gazeDirection = GazeDataRay(data, ref gazeDataWindow);
                gazeSampler[sampleCounter, 0] = gazeDirection.x;
                gazeSampler[sampleCounter, 1] = gazeDirection.y;
                gazeSampler[sampleCounter, 2] = gazeDirection.z;

                // Pupil Data Handling with LabStreaming Layer
                float[] pupilDirection = PupilData(data);
                gazeSampler[sampleCounter, 3] = pupilDirection[0];
                gazeSampler[sampleCounter, 4] = pupilDirection[1];
                gazeSampler[sampleCounter, 5] = pupilDirection[2];
                gazeSampler[sampleCounter, 6] = pupilDirection[3];

                sampleCounter++;
                if(sampleCounter >= 100)
                {
                    // Send the Chunk!
                    lslOutlet.push_chunk(gazeSampler);
                    gazeSampler = new float[100, lslChannelCount];
                    sampleCounter = 0;
                }

            }

            Thread.Sleep(FrequencyControl);
        }
    }

    private void FixedUpdate()
    {
        frameCounter++;
    }

    /// <summary>
    /// Function that recognizes different blink types - Small, Medium and Extended. Using References of Blink Data Structures to keep continuity with the Threading Operation.
    /// </summary>
    /// <param name="open_left"></param>
    /// <param name="open_right"></param>
    /// <param name="leftBlinkTracker"></param>
    /// <param name="rightBlinkTracker"></param>
    /// <param name="startTimeStamp_Left"></param>
    /// <param name="startTimeStamp_Right"></param>
    /// <param name="MAXCOUNTER_REACHED_LEFT"></param>
    /// <param name="MAXCOUNTER_REACHED_RIGHT"></param>
    void TreatBlinkData(float open_left, float open_right, ref List<float> leftBlinkTracker, ref List<float> rightBlinkTracker, ref int startTimeStamp_Left, ref int startTimeStamp_Right, ref bool MAXCOUNTER_REACHED_LEFT, ref bool MAXCOUNTER_REACHED_RIGHT)
    {
        // Treating the Left Eye 
        if ((open_left > THRESHOLD_CLOSE_EYE_VALUE && leftBlinkTracker.Count > 0) || leftBlinkTracker.Count > MAX_FRAME_COUNT)
        {
            // Check the List if the player opens eyes or if MAX_FRAME reached.
            if (leftBlinkTracker.Count > MAX_FRAME_COUNT)
            {
                // If this happens flush the List and continue as if it was nothing. A Flag will be thrown.
                leftBlinkTracker = new List<float>
                {
                    open_left
                };
                MAXCOUNTER_REACHED_LEFT = true;
            }
            else
            {
                if (leftBlinkTracker.Count < m_MinimumBlinkFrames)
                {
                    if (MAXCOUNTER_REACHED_LEFT)
                    {
                        // We need to take this into account
                        AddDataToQueue(startTimeStamp_Left, EyeID.LEFT, BlinkType.EXTENDED_BLINK);
                        MAXCOUNTER_REACHED_LEFT = false;
                    }
                    // Ignore this and start over
                    leftBlinkTracker = new List<float>();
                }
                else if (leftBlinkTracker.Count <= m_SmallBlinkFrames)
                {
                    // We need to take this into account
                    AddDataToQueue(startTimeStamp_Left, EyeID.LEFT, BlinkType.SHORT_BLINK);
                    leftBlinkTracker = new List<float>();
                }
                else if (leftBlinkTracker.Count > m_SmallBlinkFrames && leftBlinkTracker.Count <= m_MediumBlinkFrames)
                {
                    // We need to take this into account
                    AddDataToQueue(startTimeStamp_Left, EyeID.LEFT, BlinkType.MEDIUM_BLINK);
                    leftBlinkTracker = new List<float>();
                }
                else
                {
                    // We need to take this into account
                    AddDataToQueue(startTimeStamp_Left, EyeID.LEFT, BlinkType.EXTENDED_BLINK);
                    leftBlinkTracker = new List<float>();
                }
            }

        }
        else if (open_left <= THRESHOLD_CLOSE_EYE_VALUE)
        {
            // Kickstart Blink Recorder
            if (leftBlinkTracker.Count == 0)
            {
                startTimeStamp_Left = frameCounter;
            }
            // Add it to the list!
            leftBlinkTracker.Add(open_left);
        }



        // Treating the Right Eye
        if ((open_right > THRESHOLD_CLOSE_EYE_VALUE && rightBlinkTracker.Count > 0) || rightBlinkTracker.Count > MAX_FRAME_COUNT)
        {
            // Check the List if the player opens eyes or if MAX_FRAME reached.
            if (rightBlinkTracker.Count > MAX_FRAME_COUNT)
            {
                // If this happens flush the List and continue as if it was nothing. A Flag will be thrown.
                rightBlinkTracker = new List<float>
                {
                    open_right
                };
                MAXCOUNTER_REACHED_RIGHT = true;
            }
            else
            {
                if (rightBlinkTracker.Count < m_MinimumBlinkFrames)
                {
                    if (MAXCOUNTER_REACHED_RIGHT)
                    {
                        // We need to take this into account
                        AddDataToQueue(startTimeStamp_Right, EyeID.RIGHT, BlinkType.EXTENDED_BLINK);
                        MAXCOUNTER_REACHED_RIGHT = false;
                    }
                    // Ignore this and start over
                    rightBlinkTracker = new List<float>();
                }
                else if (rightBlinkTracker.Count <= m_SmallBlinkFrames)
                {
                    // We need to take this into account
                    AddDataToQueue(startTimeStamp_Right, EyeID.RIGHT, BlinkType.SHORT_BLINK);
                    rightBlinkTracker = new List<float>();
                }
                else if (rightBlinkTracker.Count > m_SmallBlinkFrames && rightBlinkTracker.Count <= m_MediumBlinkFrames)
                {
                    // We need to take this into account
                    AddDataToQueue(startTimeStamp_Right, EyeID.RIGHT, BlinkType.MEDIUM_BLINK);
                    rightBlinkTracker = new List<float>();
                }
                else
                {
                    // We need to take this into account
                    AddDataToQueue(startTimeStamp_Right, EyeID.RIGHT, BlinkType.EXTENDED_BLINK);
                    rightBlinkTracker = new List<float>();
                }
            }
        }
        else if (open_right <= THRESHOLD_CLOSE_EYE_VALUE)
        {
            // Kickstart Blink Recorder
            if (rightBlinkTracker.Count == 0)
            {
                startTimeStamp_Right = frameCounter;
            }
            // Add it to the list!
            rightBlinkTracker.Add(open_right);
        }
    }

    Vector3 GazeDataRay(EyeData eyeData, ref List<Ray> gazeDataWindow)
    {
        bool valid = eyeData.verbose_data.combined.eye_data.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY);
        Vector3 direction = Vector3.zero;

        if (valid)
        {
            // Do Stuff
            Vector3 origin = eyeData.verbose_data.combined.eye_data.gaze_origin_mm * 0.001f;
            direction = eyeData.verbose_data.combined.eye_data.gaze_direction_normalized;
            direction.x *= -1; // This needs to be reversed
            gazeDataWindow.Add(new Ray(origin, direction));
        }

        if (gazeDataWindow.Count >= m_GAZE_FRAME_WINDOW)
        {
            Vector3 sum_origin = Vector3.zero;
            Vector3 sum_direction = Vector3.zero;
            foreach (Ray r in gazeDataWindow)
            {
                sum_origin += r.origin;
                sum_direction += r.direction;
            }

            Vector3 avg_origin = new Vector3(sum_origin.x / gazeDataWindow.Count, sum_origin.y / gazeDataWindow.Count, sum_origin.z / gazeDataWindow.Count);
            Vector3 avg_direction = new Vector3(sum_direction.x / gazeDataWindow.Count, sum_direction.y / gazeDataWindow.Count, sum_direction.z / gazeDataWindow.Count);
            TreatGazeData(new Ray(avg_origin, avg_direction));

            gazeDataWindow.Clear(); // Reset
        }

        return direction;

    }

    void TreatGazeData(Ray ray)
    {
        EyeInfoData eye_info = new EyeInfoData
        {
            gaze_ray = ray,
            timestamp = frameCounter
        };
        gaze_data.Enqueue(eye_info);
    }

    float[] PupilData(EyeData eyeData)
    {
        bool valid_left = eyeData.verbose_data.left.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_PUPIL_POSITION_IN_SENSOR_AREA_VALIDITY);

        float XLeft = 0.0f;
        float YLeft = 0.0f;
        if (valid_left)
        {
            // To Make Sense in the Unity Coordinate System.
            XLeft = eyeData.verbose_data.left.pupil_position_in_sensor_area.x * 2 - 1;
            YLeft = eyeData.verbose_data.left.pupil_position_in_sensor_area.y * -2 + 1;
        }

        bool valid_right = eyeData.verbose_data.right.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_PUPIL_POSITION_IN_SENSOR_AREA_VALIDITY);
        float XRight = 0.0f;
        float YRight = 0.0f;
        if (valid_right)
        {
            // To Make Sense in the Unity Coordinate System.
            XRight = eyeData.verbose_data.right.pupil_position_in_sensor_area.x * 2 - 1;
            YRight = eyeData.verbose_data.right.pupil_position_in_sensor_area.y * -2 + 1;
        }

        return new float[] { XLeft, YLeft, XRight, YRight };

    }

    // Add Blink Type to Queue Function
    void AddDataToQueue(int timeStart, EyeID eye, BlinkType type)
    {
        BlinkData bd = new BlinkData
        {
            start_timestamp = timeStart,
            end_timestamp = frameCounter,
            eye = eye,
            type = type
        };

        blink_data.Enqueue(bd);
    }

    // This needs to be called to kickstart the LSL Stream Method.
    liblsl.StreamOutlet KickStartLSLStream()
    {
        lslStreamInfo = new liblsl.StreamInfo(
            lslStreamName,
            lslStreamType,
            lslChannelCount,
            nominal_srate,
            lslChannelFormat,
            unique_source_id);

        return new liblsl.StreamOutlet(lslStreamInfo);
    }

}
