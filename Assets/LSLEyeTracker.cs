using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL;

public class LSLEyeTracker
{

    private const string unique_source_id = "AAF904D3-7FB4-425D-A89B-1F346A3C4132";

    public string lslStreamName = "Unity_EyeTracker";
    public string lslStreamType = "LSL_BlinkData";

    private liblsl.StreamInfo lslStreamInfo;
    private liblsl.StreamOutlet lslOutlet;
    private int lslChannelCount = 2;

    //Assuming that markers are never send in regular intervalls
    private double nominal_srate = 120;

    private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_float32;

    // Start is called before the first frame update
    void Awake()
    {

        lslStreamInfo = new liblsl.StreamInfo(
                                    lslStreamName,
                                    lslStreamType,
                                    lslChannelCount,
                                    nominal_srate,
                                    lslChannelFormat,
                                    unique_source_id);

        lslOutlet = new liblsl.StreamOutlet(lslStreamInfo);
    }

    /// <summary>
    /// Send the Data in Chunks where index: 0 = Samples; 1 = Channel.
    /// </summary>
    /// <param name="chunk"></param>
    public void WriteChunk(float[,] chunk)
    {
        lslOutlet.push_chunk(chunk);
    }



    
}
