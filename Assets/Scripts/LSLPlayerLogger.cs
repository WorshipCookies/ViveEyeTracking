using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL;

public class LSLPlayerLogger : MonoBehaviour
{
    public GameObject CoinObject;
    private float[] PlayerInfoData;

    // LabStreamingLayer Integration -- Gaze and Pupil Streamer
    private const string unique_source_id = "3469CD84-E3A4-4C1C-84F1-8003485B5200";

    public string lslStreamName = "PlayerInfo_60Hz";
    public string lslStreamType = "Player_Data";

    private liblsl.StreamInfo lslStreamInfo;
    private liblsl.StreamOutlet lslOutlet;
    private const int lslChannelCount = 10;

    private double nominal_srate = 60;
    private const liblsl.channel_format_t lslChannelFormat = liblsl.channel_format_t.cf_float32;

    private GameObject PlayerObject;

    // Start is called before the first frame update
    void Start()
    {
        PlayerObject = this.gameObject;
        PlayerInfoData = new float[lslChannelCount];
        lslOutlet = KickStartPlayerLSLStream();
    }

    private void FixedUpdate()
    {
        // Get Player Data
        PlayerInfoData[0] = PlayerObject.transform.position.x;
        PlayerInfoData[1] = PlayerObject.transform.position.y;
        PlayerInfoData[2] = PlayerObject.transform.position.z;
        PlayerInfoData[3] = PlayerObject.transform.rotation.eulerAngles.x;
        PlayerInfoData[4] = PlayerObject.transform.rotation.eulerAngles.y;
        PlayerInfoData[5] = PlayerObject.transform.rotation.eulerAngles.z;
        PlayerInfoData[6] = Camera.main.transform.rotation.eulerAngles.x;
        PlayerInfoData[7] = Camera.main.transform.rotation.eulerAngles.y;
        PlayerInfoData[8] = Camera.main.transform.rotation.eulerAngles.z;

        if(CoinObject == null)
        {
            PlayerInfoData[9] = 0;
        }
        else
        {
            PlayerInfoData[9] = CoinObject.transform.childCount;
        }

        //PlayerInfoData[4] = "" + GetCoinList();
        lslOutlet.push_sample(PlayerInfoData);

        PlayerInfoData = new float[lslChannelCount];
    }

    liblsl.StreamOutlet KickStartPlayerLSLStream()
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

    private string GetCoinList()
    {
        string coinList = "";
        for(int i = 0; i < CoinObject.transform.childCount; i++)
        {
            coinList += CoinObject.transform.GetChild(i).position.x + "," + CoinObject.transform.GetChild(i).position.y + "," + CoinObject.transform.GetChild(i).position.z + ";";
        }
        return coinList;
    }

}
