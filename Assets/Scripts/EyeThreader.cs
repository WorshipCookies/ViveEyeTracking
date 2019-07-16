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


        while (true)
        {
            ViveSR.Error error = SRanipal_Eye.GetEyeData(ref data);

            if(error == ViveSR.Error.WORK)
            {
                // Eye Blinking Data Treatment.
                float open_left = data.verbose_data.left.eye_openness;
                float open_right = data.verbose_data.right.eye_openness;
                TreatBlinkData(open_left, open_right, ref leftBlinkTracker, ref rightBlinkTracker, ref startTimeStamp_Left, ref startTimeStamp_Right, ref MAXCOUNTER_REACHED_LEFT, ref MAXCOUNTER_REACHED_RIGHT);
                GazeDataRay(data, ref gazeDataWindow);
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

    void GazeDataRay(EyeData eyeData, ref List<Ray> gazeDataWindow)
    {
        bool valid = eyeData.verbose_data.combined.eye_data.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY);
        if (valid)
        {
            // Do Stuff
            Vector3 origin = eyeData.verbose_data.combined.eye_data.gaze_origin_mm * 0.001f;
            Vector3 direction = eyeData.verbose_data.combined.eye_data.gaze_direction_normalized;
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

}
