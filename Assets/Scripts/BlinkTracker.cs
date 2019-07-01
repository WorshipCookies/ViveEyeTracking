using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkTracker
{
    public struct BlinkData
    {
        public BlinkType type;
        public int start_timestamp;
        public int end_timestamp;
        public EyeID eye;
    }

    public enum BlinkType
    {
        SHORT_BLINK, MEDIUM_BLINK, EXTENDED_BLINK
    }
    public enum EyeID
    {
        LEFT, RIGHT
    }

}
