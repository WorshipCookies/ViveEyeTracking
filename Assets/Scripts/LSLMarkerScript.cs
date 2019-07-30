using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL;

public class LSLMarkerScript : MonoBehaviour
{
    private const string unique_source_id = "4EE36484-D748-42F7-8A05-A6B86FF09F52";

    public string lslStreamName = "Unity_GameMarkers";
    public string lslStreamType = "LSL_Marker_Strings";

    private liblsl.StreamInfo lslStreamInfo;
    private liblsl.StreamOutlet lslOutlet;
    private int lslChannelCount = 1;

    //Assuming that markers are never send in regular intervalls
    private double nominal_srate = liblsl.IRREGULAR_RATE;

    private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_string;

    private string[] sample;

    private void Awake()
    {
        lslStreamInfo = new liblsl.StreamInfo(
                                        lslStreamName,
                                        lslStreamType,
                                        lslChannelCount,
                                        nominal_srate,
                                        lslChannelFormat,
                                        unique_source_id);

        lslOutlet = new liblsl.StreamOutlet(lslStreamInfo);
        sample = new string[lslChannelCount];
    }

    public void PushData(string data)
    {
        sample[0] = data;
        lslOutlet.push_sample(sample);
    }
}
