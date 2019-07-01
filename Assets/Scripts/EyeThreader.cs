using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Threading;
using System.IO;
using ViveSR.anipal.Eye;
using System.Collections.Concurrent;
using static BlinkTracker;

public class EyeThreader : MonoBehaviour
{
    public EyeData data = new EyeData();
    private Thread thread;

    private const int FrequencyControl = 1;
    private const int MaxFrameCount = 3600;

    public ConcurrentQueue<BlinkData> blink_data = new ConcurrentQueue<BlinkData>();
    public const int MAX_FRAME_COUNT = 7200;
    public const float THRESHOLD_CLOSE_EYE_VALUE = 0.3f;

    public float m_MinimumBlinkFrames = 6; // The minimum number of frames necessary to record a blink.
    public float m_SmallBlinkFrames = 36; // Default is 0.3 seconds.
    public float m_MediumBlinkFrames = 84; // Default is 0.7 seconds -- After that its all Extended blinks.

    [HideInInspector] public volatile int frameCounter;

    void Start()
    {
        frameCounter = 0;
        //thread = new Thread(QueryEyeData);
        thread = new Thread(TrackBlinkingData);
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

    void TrackBlinkingData()
    {

        // Left Eye Info
        List<float> leftBlinkTracker = new List<float>();
        int startTimeStamp_Left = 0;

        // Right Eye Info
        List<float> rightBlinkTracker = new List<float>();
        int startTimeStamp_Right = 0;

        // Auxilliary Variables
        bool MAXCOUNTER_REACHED_LEFT = false;
        bool MAXCOUNTER_REACHED_RIGHT = false;

        while (true)
        {
            ViveSR.Error error = SRanipal_Eye.GetEyeData(ref data);

            if(error == ViveSR.Error.WORK)
            {

                float open_left = data.verbose_data.left.eye_openness;
                float open_right = data.verbose_data.right.eye_openness;

                // Treating the Left Eye 
                if ((open_left > THRESHOLD_CLOSE_EYE_VALUE && leftBlinkTracker.Count > 0) || leftBlinkTracker.Count > MAX_FRAME_COUNT)
                {
                    // Check the List if the player opens eyes or if MAX_FRAME reached.
                    if (leftBlinkTracker.Count > MAX_FRAME_COUNT)
                    {
                        // If this happens flush the List and continue as if it was nothing. A Flag will be thrown.
                        leftBlinkTracker = new List<float>();
                        leftBlinkTracker.Add(open_left);
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
                        rightBlinkTracker = new List<float>();
                        rightBlinkTracker.Add(open_right);
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
            Thread.Sleep(FrequencyControl);
        }
    }

    private void FixedUpdate()
    {
        frameCounter++;
    }

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
