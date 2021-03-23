using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class WebRequest
{
    public WebRequest(int opCode, string url)
    {
        this.opCode = opCode;
        this.url = url;

    }

    public string url;
    public int opCode;
}

[System.Serializable]
public class PostRequest : WebRequest
{
    public WWWForm form;

    public PostRequest(int opCode, string url) : base(opCode, url)
    {
        form = new WWWForm();
    }

}

[System.Serializable]
public class WebResponse
{
    public int opCode;
    public long responseCode;
    public string data;

    public WebResponse(int opCode)
    {
        this.opCode = opCode;
        this.responseCode = 0;
        this.data = "{}";
    }
}

public class WebRequestManager1 : MonoBehaviour
{
    public static WebRequestManager1 instance = null;
    public static bool DebugMode = false;
    public delegate void WebRequestEventHandler(WebResponse res);
    public static event WebRequestEventHandler onWebResponse;
    public int WorldColor1 = 0, WorldColor2 = 1, WorldColor3 = 2, WorldFeedbackRate1 = 0, WorldFeedbackRate2 = 50, WorldFeedbackRate3 = 100 , WorldDashboardRate1 = 0, WorldDashboardRate2 = 50, WorldDashboardRate3 = 100;

    private int index = 0;
    private string str = "";
    private int PlayerGroup = 0;


    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }



    public IEnumerator sendData(PostRequest req)
    {
        UnityWebRequest www = UnityWebRequest.Post(req.url, req.form);
        //yield return www.Send();
        yield return www.SendWebRequest();
        WebResponse res = new WebResponse(req.opCode);
        if (www.isNetworkError)
        {
            res.data = www.error;
        }
        else
        {
            res.responseCode = www.responseCode;
            res.data = www.downloadHandler.text;
        }
        if (onWebResponse != null)
            onWebResponse(res);
    }



    public IEnumerator fetchData(WebRequest req)
    {
        UnityWebRequest www = UnityWebRequest.Get(req.url);
        yield return www.SendWebRequest();
        WebResponse res = new WebResponse(req.opCode);
        if (www.isNetworkError)
        {
            res.data = www.error;
        }
        else
        {
            res.responseCode = www.responseCode;
            res.data = www.downloadHandler.text;
        }
        if (onWebResponse != null)
            onWebResponse(res);
    }

    /// <DEMO EXPERIMENT>
    /// ///
    /// </THIS DEMO SHOWS A SIMPLE EXPERIMENT INCLUDING MULTIPLE STAGES>
    /// First, during the training phase, players experience two worlds. One where information about ferry speed is shared, and another world
    /// where players do not collaborate and no information about ferry speeds is available. After playing 5 islands in each world (both world colors and collaboration
    /// are randomely assigned), there are two experimental stages. In one, the player choose where to play. In the second the player is assigned with a world.
    /// <returns></returns>

    public IEnumerator Init() // SET THE GLOBAL EXPERIMENTAL VARIABLES AND SET FIRST WORLD
    {
        // randomly assign groups:
        int i = Random.Range(0, 2); // gives 0,1
        if (i == 0)
            PlayerGroup = 0;
        else
            PlayerGroup = 1;


        // randomly assign player to green or red islands
        i = Random.Range(0, 2); // gives 0,1
        switch (i)
        {
            case 0:
                WorldColor1 = 1; WorldColor2 = 2;
                break;
            case 1:
                WorldColor1 = 2; WorldColor2 = 1;
                break;
        }

        // randomly assign player to start playing in either a collaborative or in a non collaborative world: 
        i = Random.Range(0, 2);
        switch (i)
        {
            case 0:
                WorldFeedbackRate1 = 0; WorldFeedbackRate2 = 100; WorldDashboardRate1 = 0; WorldDashboardRate2 = 100;
                break;
            case 1:
                WorldFeedbackRate1 = 100; WorldFeedbackRate2 = 0; WorldDashboardRate1 = 100; WorldDashboardRate2 = 0;
                break;
        }

        // Set the first worlds
        GameManager.instance.dashboardJson.contents.island_color = WorldColor1;
        GameManager.instance.dashboardJson.contents.dashboard_rate = WorldDashboardRate1;
        GameManager.instance.dashboardJson.contents.feedback_rate = WorldFeedbackRate1;
        // HERE YOU NEED TO IMPLEMENT YOUR SERVER SIDE WITH YIELD RETURN
        // now we should have received a json from the server, which is simulated below
        str = JsonUtility.ToJson(GameManager.instance.dashboardJson);
        GameManager.instance.AfterPageUpdate(str);
        SubmitMessage("first world");
        yield return new WaitForSeconds(0.01f); // this is just a junk yeld return, which should be replace with your server side code 
    }



    public IEnumerator GetPage() // Get json data from server
    {
        index++;

        if (index == 5) // switch player to the second world after 5 islands
        {
            GameManager.instance.dashboardJson.contents.island_color = WorldColor2;
            GameManager.instance.dashboardJson.contents.dashboard_rate = WorldDashboardRate2;
            GameManager.instance.dashboardJson.contents.feedback_rate = WorldFeedbackRate2;
            str = JsonUtility.ToJson(GameManager.instance.dashboardJson);
            GameManager.instance.controller.enabled = false; // player lose control on movement
            GameManager.instance.AfterPageUpdate(str);
            SubmitMessage("second world");
        }

        if (index == 10 || index == 15 || index == 20 || index == 25)
        {

            if (WorldFeedbackRate1 == 100)
                WorldFeedbackRate1 = -1;
            if (WorldFeedbackRate2 == 100)
                WorldFeedbackRate2 = -1;

            if (PlayerGroup == 0 && (index == 10 || index == 15))
                GameManager.instance.AssignWorld = true; //  group 0 start with world being assigned 
            if (PlayerGroup == 0 && (index == 20 || index == 25))
                GameManager.instance.AssignWorld = false; //  group 0 end with two world choices
            if (PlayerGroup == 1 && (index == 10 || index == 15))
                GameManager.instance.AssignWorld = false; //  group 1 start with two world choices
            if (PlayerGroup == 1 && (index == 20 || index == 25))
                GameManager.instance.AssignWorld = true; //  group 1 end with world being assigned 

            GameManager.instance.dashboardJson.attributes.type = "UnityQuestionPage";
            str = JsonUtility.ToJson(GameManager.instance.dashboardJson);
            GameManager.instance.controller.enabled = false; // player lose control on movement
            GameManager.instance.AfterPageUpdate(str);
        }

        if (index == 30)
            GameManager.instance.FinishGame();

        yield return new WaitForSeconds(0.01f);
    }



    public IEnumerator SubmitPage(string answerJson, string metadataJson) // Send JSON data to server
    {
        // save the json  
        const string URL = Constants.URLBASE + "/api/gameaction";
        PostRequest reqParams = new PostRequest(1, URL);
        reqParams.form.AddField("val", answerJson);
        reqParams.form.AddField("code", 1.ToString());
        StartCoroutine(sendData(reqParams)); // save player performance in island
        GameManager.instance.AfterPageSubmitted(); // now GameManager will call getPage, where game logic take place (replacing Psynet logic)
        yield return new WaitForSeconds(0.01f);

    }

    public void SubmitMessage(string msg) // Send JSON data to PsyNet
    { 
        const string URL = Constants.URLBASE + "/api/gameaction";
        PostRequest reqParams = new PostRequest(1, URL);
        reqParams.form.AddField("val", msg);
        reqParams.form.AddField("code", 2.ToString());
        StartCoroutine(sendData(reqParams)); 

    }
}





