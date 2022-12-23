using UnityEngine;
using UnityEngine.UI;
using GME;
using System;

/// <summary>
/// GmeVoiceChatScript
/// </summary>
public class GmeVoiceChatScript : MonoBehaviour
{
    public InputField userInputField;
    public Text logText;
    
    protected string _log;
    protected int _logCnt = 0;
    protected int _maxCnt = 20;

    public string roomId = "dev_room";
    public string openID = "20221225";      // INT64 

    string sdkAppId = "XXXXXXXXXX";         // Tencent Account [GME AppID]
    string authkey = "XXXXXXXXXXXXXXXX";    // Tencent Account [GME Permission key]

    static int sUid = 0;

    ITMGRoomType _roomType = ITMGRoomType.ITMG_ROOM_TYPE_FLUENCY;
    string _speechLanguage = "ja-JP";

#region << Property >>

    public bool IsInit
    {
        get;
        private set;
    }

    public bool IsRoom
    {
        get;
        private set;
    }

    public bool IsRecord
    {
        get;
        private set;
    }

    public bool IsInitSpeak
    {
        get;
        private set;
    }

    public bool IsSpeak
    {
        get;
        private set;
    }

#endregion << Property >>

    /// <summary>
    /// Update
    /// </summary>
    void Update()
    {
        if (this.IsInit) {
            ITMGContext.GetInstance().Poll();
        }
    }

    /// <summary>
    /// OnDestroy
    /// </summary>
    private void OnDestroy()
    {
        GmeClose(false);
    }

    /// <summary>
    /// GmeCloseClick
    /// </summary>
    public void GmeCloseClick()
    {
        GmeClose();
    }

    /// <summary>
    /// GmeInitClick
    /// </summary>
    public void GmeInitClick()
    {
        if (userInputField == null) {
            return;
        }
        var user = userInputField.text;
        if (string.IsNullOrEmpty(user)) {
            return;
        }

        GmeOpen(user);
    }

    /// <summary>
    /// LogClearClick
    /// </summary>
    public void LogClearClick()
    {
        try
        {
            _log = string.Empty;
            if (logText != null)
                logText.text = string.Empty;
        }
        catch 
        {            
        }
    }

    /// <summary>
    /// AddLogData
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="isWarning"></param>
    public void AddLogData(string msg, bool isWarning = false)
    {
        try {
            if (isWarning) {
                Debug.LogWarning(msg);
            } else {
                Debug.Log(msg);
            }


            _logCnt++;

            if (_logCnt <= 1) {
                _log = msg;
            } else if (_logCnt > _maxCnt) {
                _logCnt = 0;
                _log = msg;
            } else {
                _log += "\r\n" + msg;
            }

            if (logText != null)
                logText.text = _log;
        } catch { }
    }


    /// <summary>
    /// GmeOpen
    /// </summary>
    public void GmeOpen()
    {
        var date = DateTime.Now;
        this.openID = string.Format("{0:D2}{1:D2}{2:D2}{3:D2}{4:D3}", date.Month, date.Day, date.Hour, date.Minute, date.Millisecond);
        GmeOpen(openID, _roomType, _speechLanguage);
    }

    /// <summary>
    /// GmeOpen
    /// </summary>
    /// <param name="id"></param>
    public void GmeOpen(string id)
    {
        this.openID = id;
        GmeOpen(openID, _roomType, _speechLanguage);
    }

    /// <summary>
    /// GmeOpen
    /// </summary>
    /// <param name="sRoomID"></param>
    /// <param name="roomType"></param>
    /// <param name="speechLanguage"></param>
    protected void GmeOpen(string sOpenID, ITMGRoomType roomType, string speechLanguage)
    {
        var instance = ITMGContext.GetInstance();
        if (instance == null) {
            return;
        }


        //------------------------------------------------------------
        AddLogData("Init");
        int ret = instance.Init(sdkAppId, sOpenID);
        if (ret != QAVError.OK) {
            AddLogData("SDK initialization failed:" + ret, true);
            return;
        }

        LoginFunction();

        this.IsInit = true;

        byte[] authBuffer = GetAuthBuffer(sdkAppId, roomId, sOpenID, authkey);

        //------------------------------------------------------------
        AddLogData("EnterRoom");
        ret = instance.EnterRoom(roomId, roomType, authBuffer);
        if (ret != QAVError.OK) {
            AddLogData("EnterRoom failed:" + ret, true);
            return;
        }

        this.IsRoom = true;

        //------------------------------------------------------------
        AddLogData("ApplyPTTAuthbuffer");
        instance.GetPttCtrl().ApplyPTTAuthbuffer(authBuffer);

        //------------------------------------------------------------
        AddLogData("StartRecordingWithStreamingRecognition");
        string recordPath = Application.persistentDataPath + string.Format("/{0}.silk", sUid++);
        ret = instance.GetPttCtrl().StartRecordingWithStreamingRecognition(recordPath, speechLanguage, speechLanguage);
        if (ret != 0) {
            AddLogData("StartRecordingWithStreamingRecognition failed:" + ret, true);
            return;
        }
        this.IsRecord = true;

        EnterJoinFunction();
    }

    /// <summary>
    /// GmeClose
    /// </summary>
    /// <param name="isLog"></param>
    public void GmeClose(bool isLog = true)
    {
        var instance = ITMGContext.GetInstance();
        if (instance == null) {
            return;
        }

        if (this.IsRecord) {
            this.IsRecord = false;
            if (isLog)
                AddLogData("StopRecording");
            instance.GetPttCtrl().StopRecording();
        }

        if (this.IsRoom) {
            this.IsRoom = false;
            if (isLog)
                AddLogData("ExitRoom");
            instance.ExitRoom();
        }

        if (this.IsInit) {
            this.IsInit = false;
            if (isLog)
                AddLogData("Uninit");
            LeaveJoinFunction();
            LeaveFunction();

            instance.Uninit();
        }
    }

    /// <summary>
    /// LoginFunction
    /// </summary>
    private void LoginFunction()
    {
        var instance = ITMGContext.GetInstance();
        if (instance != null) {
            instance.OnEnterRoomCompleteEvent += new QAVEnterRoomComplete(OnEnterRoomComplete);
            instance.OnExitRoomCompleteEvent += new QAVExitRoomComplete(OnExitRoomComplete);
            instance.OnRoomDisconnectEvent += new QAVRoomDisconnect(OnRoomDisconnect);
            instance.OnEndpointsUpdateInfoEvent += new QAVEndpointsUpdateInfo(OnEndpointsUpdateInfo);
        }
    }

    /// <summary>
    /// LeaveFunction
    /// </summary>
    private void LeaveFunction()
    {
        var instance = ITMGContext.GetInstance();
        if (instance != null) {
            instance.OnEnterRoomCompleteEvent -= new QAVEnterRoomComplete(OnEnterRoomComplete);
            instance.OnExitRoomCompleteEvent -= new QAVExitRoomComplete(OnExitRoomComplete);
            instance.OnRoomDisconnectEvent -= new QAVRoomDisconnect(OnRoomDisconnect);
            instance.OnEndpointsUpdateInfoEvent -= new QAVEndpointsUpdateInfo(OnEndpointsUpdateInfo);
        }
    }

    /// <summary>
    /// EnterJoinFunction
    /// </summary>
    private void EnterJoinFunction()
    {
        var ctl = ITMGContext.GetInstance().GetPttCtrl();
        if (ctl != null) {
            ctl.OnStreamingSpeechComplete += new QAVStreamingRecognitionCallback(OnStreamingRecComplete);
            ctl.OnStreamingSpeechisRunning += new QAVStreamingRecognitionCallback(OnStreamingRecisRunning);
        }
    }

    /// <summary>
    /// LeaveJoinFunction
    /// </summary>
    private void LeaveJoinFunction()
    {
        var ctl = ITMGContext.GetInstance().GetPttCtrl();
        if (ctl != null) {
            ctl.OnStreamingSpeechComplete += new QAVStreamingRecognitionCallback(OnStreamingRecComplete);
            ctl.OnStreamingSpeechisRunning += new QAVStreamingRecognitionCallback(OnStreamingRecisRunning);
        }
    }

    /// <summary>
    /// OnEnterRoomComplete
    /// </summary>
    /// <param name="result"></param>
    /// <param name="error_info"></param>
    public void OnEnterRoomComplete(int result, string error_info)
    {
        AddLogData(string.Format("OnEnterRoomComplete = {0}", result));
        if (result != 0) {
            return;
        }

        var audioCtl = ITMGContext.GetInstance().GetAudioCtrl();
        if (audioCtl != null) {
            var ret = audioCtl.EnableMic(true);
            AddLogData(string.Format("EnableMic = {0}", ret));

            var ret2 = audioCtl.EnableSpeaker(true);
            AddLogData(string.Format("EnableSpeaker = {0}", ret2));

            if (ret == 0 && ret2 == 0) {
                this.IsInitSpeak = true;
            }
        }
    }

    /// <summary>
    /// OnExitRoomComplete
    /// </summary>
    public void OnExitRoomComplete()
    {
        AddLogData("OnExitRoomComplete");

        var audioCtl = ITMGContext.GetInstance().GetAudioCtrl();
        if (audioCtl != null) {
            var ret = audioCtl.EnableMic(false);
            AddLogData(string.Format("DisableMic = {0}", ret));

            ret = audioCtl.EnableSpeaker(false);
            AddLogData(string.Format("DisableSpeaker = {0}", ret));

            this.IsInitSpeak = false;
        }
    }

    public void OnRoomDisconnect(int result, string error_info)
    {
        AddLogData(string.Format("OnRoomDisconnect = {0}", result));
    }

    /// <summary>
    /// OnEndpointsUpdateInfo
    /// </summary>
    /// <param name="eventID"></param>
    /// <param name="count"></param>
    /// <param name="openIdList"></param>
    public void OnEndpointsUpdateInfo(int eventID, int count, string[] openIdList)
    {
        const int ITMG_EVENT_ID_USER_ENTER = 1;
        const int ITMG_EVENT_ID_USER_EXIT = 2;
        const int ITMG_EVENT_ID_USER_HAS_AUDIO = 5;
        const int ITMG_EVENT_ID_USER_NO_AUDIO = 6;

        string strEvent = "unknown";
        switch(eventID)
        {
            case ITMG_EVENT_ID_USER_ENTER:
                strEvent = "USER_ENTER";
                break;

            case ITMG_EVENT_ID_USER_EXIT:
                strEvent = "USER_EXIT";
                break;

            case ITMG_EVENT_ID_USER_HAS_AUDIO:
                strEvent = "USER_HAS_AUDIO";
                this.IsSpeak = true;
                break;

            case ITMG_EVENT_ID_USER_NO_AUDIO:
                strEvent = "USER_NO_AUDIO";
                this.IsSpeak = false;
                break;
        }

        AddLogData(string.Format("OnEndpointsUpdateInfo : eventID = {0}", strEvent));
    }

    public byte[] GetAuthBuffer(string sdkAppID, string roomID, string userID, string authKey)
    {
        return QAVAuthBuffer.GenAuthBuffer(int.Parse(sdkAppID), roomID, userID, authKey);
    }

    void OnStreamingRecComplete(int code, string fileid, string filePath, string result)
    {
        AddLogData(string.Format("OnStreamingRecComplete = {0}, {1}", code, fileid));
    }

    void OnStreamingRecisRunning(int code, string fileid, string filePath, string result)
    {
        AddLogData(string.Format("OnStreamingRecisRunning = {0}, {1}", code, result));
    }
}
