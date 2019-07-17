using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL;

public class LSLInterface
{

    //private const string unique_source_id = "AAF904D3-7FB4-425D-A89B-1F346A3C4132";

    public string lslStreamName;
    public string lslStreamType;

    private liblsl.StreamInfo lslStreamInfo;
    private liblsl.StreamOutlet lslOutlet;
    private int lslChannelCount;

    //Assuming that markers are never send in regular intervalls
    private double nominal_srate;

    //private const liblsl.channel_format_t lslChannelFormat;


    public LSLInterface(string unique_source_id, string lslStreamName, string lslStreamType, int lslChannelCount, double nominal_srate, 
        liblsl.channel_format_t lslChannelFormat)
    {
        this.lslStreamName = lslStreamName;
        this.lslStreamType = lslStreamType;
        this.lslChannelCount = lslChannelCount;
        this.nominal_srate = nominal_srate;

        // Constructor goes here
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
