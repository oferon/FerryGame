using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    // public vars for game objects (mostly visual) components scripting
    public static GameManager instance = null;
    public Settings GameOptions; // This is the game options class, see declaration in settings.cs
    public GameObject ferryR, ferryG, pollR, pollG, Bridges;
    public GameObject player, obstracles, Tutorial, FinishGameOption;
    public GameObject FerrySign, ShowRatingTrends, ShowCurrentRatings, FerryOptions, DashboardOptions, RatingsOptions, CoinsOptions, RatingsGreen, TrendsGreen;
    public GameObject FeedbackDevice, NoRatings1, NoRatings2, TrendmeObj, TrendmeObj2, TrendGraph1, TrendGraph2;
    public Camera TrendsCam;
    public GameObject Dashboard, SettingsPage;
    public GameObject[] Slides = new GameObject[6]; // for showing game tutorial slides before the game
    public RigidbodyFirstPersonController controller;
    public GameObject wall;
    public GameObject[] Trees = new GameObject[9];
    public GameObject[] Coins = new GameObject[15];
    public Text CoinsCount, submitBtnText;
    public GameObject RedFerryButton, GreenFerryButton;
    public GameObject LightR, LightG;
    public int Score = 0;
    public Slider FeedbackSlider, DashboardRedFerrySlider, DashboardYellowFerrySlider, DashboardGreenFerrySlider;
    public GameObject feedbackChoice;
    public GameObject FeebackChoiceButton1, FeedbackChoiceButton2, FeedbackWait, SelectTheWorld;
    public GameObject sliderbuttonUP, sliderbuttonDown;
    public Text WorldInfo, SessionTimerText;
    public int World = 0;
    public Button WorldOption1, WorldOption2, WorldOption3;
    public Metadata metadata = new Metadata();
    public Answer answer = new Answer();
    public Dropdown DDWorld, DDRatingDevice, DDShowDashboard, DDDashboardSNR, DDRequestFeedback, DDDashboardType, DDNumFerries, DDDynamicFerryIdentity, DDFerrySpeedAssignment, DDPresentBridge;
    public Slider SLFastFerryTopSpeed, SLFastFerryMinSpeed, SLSlowFerryTopSpeed, SLSlowFerryMinSpeed, SLTrendResolution, SLRatingDelay, SLSliderFriction;
    public int trialDecrement = 0;
    public Toggle RatingToggle;
    public Text SelectWorldText;
    public Button chooseWorldBtn1, chooseWorldBtn2;
    public bool WaitforPsynet = false, AssignWorld = false;

    // global class private vars
    private GameObject activeLight; // the blinking light of an active ferry
    private bool FerryChoiceTime, FerryCalled, showFerry, FerryRidingR, FerryRidingG, FeedbackTime = false, FeedbackButtonHighPressed = false, FeedbackButtonLowPressed = false, FeedbackSubmitted = false;
    private bool FeedbackLeftKeyIsDown = false, FeedbackRightKeyIsDown = false, _waitNow = false, ShowtrendTimer = false;
    private double time_game = 0, speedR, speedG, movedR, movedG, ridenR, ridenG, blink;
    private double _waitProgress = 0;
    private float _feedbackProgress = 0;
    private int trial = 1, ferryChoice;
    private double ferryGspeed, ferryRspeed, currentTime;
    private bool DashboardActive = true;
    public DashboardJson dashboardJson = new DashboardJson();
    public QuestionJson questionJson = new QuestionJson();
    private Vector3[] objectsDefaultLocations = new Vector3[26];
    private bool clearCoins = false;
    private int current_world = -1;
    private double SessionTimertime = 10.0;
    private bool GameActive = true, SessionTimer = false;
    private int worldIndex =0;
    private bool[] assignedWorldOrder = new bool [2]; // for a demo experiment
    private int WorldAssignmentIndex = 0;


    void OnEnable()
    {
        WebRequestManager1.onWebResponse += HandleWebResult;
    }

    void OnDisable()
    {
        WebRequestManager1.onWebResponse -= HandleWebResult;
    }


    private void HandleWebResult(WebResponse res)
    {
        int opcode = res.opCode;
        long resCode = res.responseCode;
        switch (opcode)
        {
            case Constants.SCORESUBMIT_OPCODE:
                if (resCode == 200)
                {
                    Application.OpenURL("http://[YOUR SERVER URL HERE].php");
                }
                break;
            default:
                break;
        }
    }




    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // set random assigment of worlds
        int i = Random.Range(0, 2); // gives 0,1
        if (i == 0)
        {
            assignedWorldOrder[0] = false; assignedWorldOrder[1] = true;
        }
        else
        {
            assignedWorldOrder[0] = true; assignedWorldOrder[1] = false;
        }
        SetRatingDevice();
        ShowBridges();
        ResetFerryVars();
        GetObjectsLocation();
        if (GameOptions.Standalone) // desgin mode
        {
            // show settings if standalone
            SettingsPage.SetActive(true);
            Tutorial.SetActive(false);
        }
        else // experiment mode
        {
            SettingsPage.SetActive(false);
            Tutorial.SetActive(true);
            StartCoroutine(WebRequestManager1.instance.Init());
        }
        controller.enabled = false;
    }



    void ResetFerryVars()
    {
        FerrySign.SetActive(false);
        FeedbackDevice.SetActive(false);
        RedFerryButton.SetActive(false);
        GreenFerryButton.SetActive(false);
        Dashboard.SetActive(false);
        FerryChoiceTime = false;
        FerryCalled = false;
        showFerry = false;
        FerryRidingR = FerryRidingG = false;
    }



    // Update is called once per frame
    void Update()
    {
        float dTime = Time.deltaTime;
        time_game += dTime;

        // work around a display bug in 3rd party trend graph (only applicable to show trend game option):
        if (ShowtrendTimer)
        {
            if (time_game - currentTime < 0.5)
                TrendsCam.gameObject.SetActive(false);
            if (time_game - currentTime > 0.5 && time_game - currentTime < 1)
            {
                TrendsCam.gameObject.SetActive(true);
                if (!GameOptions.TwoFerries)
                    TrendsGreen.SetActive(false);
            }

            if (time_game - currentTime > 1.2)
            {
                trendplot();
                ShowtrendTimer = false;
            }
        }

        // manage session timer
        if (SessionTimer)
        {
            SessionTimertime -= dTime;
            SessionTimerText.text = "00:00:" + (int)SessionTimertime;
            if (SessionTimertime < 0)
            {
                TutorialNext(5);
                SessionTimer = false;
            }
        }


        // manage waiting time cost for feedback ratings
        if (_waitNow)
        {
            _waitProgress += dTime;
            if (_waitProgress > GameOptions.ratingDelay)
            {
                _waitNow = false;
                _waitProgress = 0;
                feedbackChoice.SetActive(false);
            }
        }

        // manage feedback slider dynamics
        if (FeedbackDevice.activeInHierarchy)
        {
            SetFeedbackSiderKeys(); // allow keys to replace mouse for feedback device
                                    // manage costly slider up friction
            if (FeedbackButtonHighPressed || FeedbackRightKeyIsDown)
            {
                submitBtnText.text = "Submit";
                _feedbackProgress += dTime;
                IncrementFeedbackSlider();
            }
            // manage costly slider down friction
            if (FeedbackButtonLowPressed || FeedbackLeftKeyIsDown)
            {
                submitBtnText.text = "Submit";
                _feedbackProgress += dTime;
                DecrementFeedbackSlider();
            }
        }


        // time to choose a ferry by clicking a buttons
        if (FerryChoiceTime)
        {
            DisplayDashboardInformation();  // Show or hide dashboard information
            MoveObjects();// move trees and coins to the new island
            PositionFerries(); // position ferries in front of player (across the water)
            FerryChoiceTime = false; // call this only once
        }

        // manage ferry movements once player chose a ferry to ride
        if (FerryCalled)
        {
            if (showFerry) // this happans only once
            {
                ActivateFerries();
                showFerry = false;
                //Debug.Log("SNR="); Debug.Log(GameOptions.dashboardSNR);
            }
            blink += dTime; // the chosen ferry light are blinking
            BlinkFerryLights();
            MoveFerriesTowardPlayer(dTime);
        }
        MoveFerriesTowardNextIslant(dTime);// now move the ferry to the next island

        // Handle feedback:
        if (FeedbackTime)
            HandleFeedback();
        else if (FeedbackSubmitted) // reset variables once feedback is handled
        {
            trial++;
            FerryRidingR = FerryRidingG = false;
            movedR = movedG = ridenR = ridenG = 0;
            FeedbackSlider.value = 50;
            controller.enabled = true;
            FeedbackSubmitted = false;
        }
    }



    private void SetFeedbackSiderKeys()
    {
        // allow keys to replace mouse for feedback device
        if (Input.GetKeyDown("left"))
            FeedbackLeftKeyIsDown = true;
        if (Input.GetKeyUp("left"))
            FeedbackLeftKeyIsDown = false;
        if (Input.GetKeyDown("right"))
            FeedbackRightKeyIsDown = true;
        if (Input.GetKeyUp("right"))
            FeedbackRightKeyIsDown = false;
    }


    private void IncrementFeedbackSlider()
    {
        if (FeedbackSlider.value < 81 && _feedbackProgress > 0.025) // comfortable friction at the center
        {
            _feedbackProgress = 0;
            FeedbackSlider.value++;
        }
        else if (FeedbackSlider.value > 80 && FeedbackSlider.value < 91 && _feedbackProgress > GameOptions.sliderFriction)
        {
            _feedbackProgress = 0;
            FeedbackSlider.value++;
        }
        else if (FeedbackSlider.value > 90 && _feedbackProgress > 2.5 * GameOptions.sliderFriction)
        {
            _feedbackProgress = 0;
            FeedbackSlider.value++;
        }
    }


    private void DecrementFeedbackSlider()
    {
        if (FeedbackSlider.value > 19 && _feedbackProgress > 0.025) // comfortable friction at the center
        {
            _feedbackProgress = 0;
            FeedbackSlider.value--;
        }
        else if (FeedbackSlider.value < 20 && FeedbackSlider.value > 9 && _feedbackProgress > GameOptions.sliderFriction)
        {
            _feedbackProgress = 0;
            FeedbackSlider.value--;
        }
        else if (FeedbackSlider.value < 10 && _feedbackProgress > 2.5 * GameOptions.sliderFriction)
        {
            _feedbackProgress = 0;
            FeedbackSlider.value--;
        }
    }


    private void DisplayDashboardInformation()
    {
        RatingToggle.isOn = false;
        RatingToggle.interactable = true;

        RedFerryButton.SetActive(true);
        if (GameOptions.TwoFerries)
            GreenFerryButton.SetActive(true);
        if (DashboardActive)
        {
            currentTime = time_game;
            Dashboard.SetActive(true);
            if (!GameOptions.TwoFerries)
                RatingsGreen.SetActive(false);
            if (GameOptions.dashboardType == DashboardType.Trends)
                ShowtrendTimer = true;
            // decide if to show left ferry ratings
            if (ShowPlayerDashboardInfo())
            {
                NoRatings1.SetActive(false);
                answer.showDashboard1 = false;
            }
            else
            {
                NoRatings1.SetActive(true);
                answer.showDashboard1 = true;
            }
            // decide if to show right ferry ratings
            if (ShowPlayerDashboardInfo())
            {
                NoRatings2.SetActive(false);
                answer.showDashboard2 = false;
            }
            else
            {
                NoRatings2.SetActive(true);
                answer.showDashboard2 = true;
            }
        }
        else
        {
            {
                Dashboard.SetActive(false);
                answer.showDashboard1 = false;
                answer.showDashboard2 = false;
            }
        }
    }


    private void PositionFerries()
    {
        ferryR.transform.position = player.transform.position;
        ferryG.transform.position = player.transform.position;
        ferryR.transform.Translate(0, (float)-0.5, -12); // place the red ferry just opposed to player
        ferryG.transform.Translate(0, (float)-0.5, -12); // place the green ferry just opposed to player
        movedR = movedG = ridenR = ridenG = 0; // reset ferry movement
        showFerry = true;
    }


    private void ActivateFerries()
    {
        FerrySign.SetActive(false);
        ferryR.SetActive(true);
        if (GameOptions.TwoFerries)
            ferryG.SetActive(true);
        switch (ferryChoice)
        {
            case 0: // position the other ferry to the left
                ferryG.transform.Translate(-3, 0, 0);
                break;
            case 2: // position the other ferry to the right
                ferryR.transform.Translate(3, 0, 0);
                break;
        }
    }

    private void BlinkFerryLights()
    {
        if (blink > 0.5)
        {
            blink = 0;
            if (activeLight.activeInHierarchy)
                activeLight.SetActive(false);
            else
                activeLight.SetActive(true);
        }
    }


    private void MoveFerriesTowardPlayer(float dTime)
    {
        float move;
        if (movedR < 9.5) // move ferry toward player for fixed distance (9.5)
        {
            move = (float)speedR * dTime;
            ferryR.transform.Translate(0, 0, move);
            movedR += move;
        }
        else
            FerryRidingR = true; // time for ferry to move toward the next island ('riding mode')


        if (movedG < 9.5) // move ferry toward player
        {
            move = (float)speedG * dTime;
            ferryG.transform.Translate(0, 0, move);
            movedG += move;
        }
        else
            FerryRidingG = true; // time for ferry to move toward the next island ('riding mode')
    }


    private void MoveFerriesTowardNextIslant(float dTime)
    {
        float move;
        if (FerryRidingR)
        {
            if (ridenR < 12) // move the red ferry toward next island for a fixed distance (9.5)
            {
                move = (float)speedR * dTime;
                ferryR.transform.Translate(0, 0, -move);
                ridenR += move;
                if (ferryChoice == 0)
                    player.transform.Translate(0, 0, move);
            }
            else if (ferryChoice == 0)
                FeedbackTime = true;
        }
        if (FerryRidingG)
        {
            if (ridenG < 12) // move the green ferry toward next island for a fixed distance (9.5)
            {
                move = (float)speedG * dTime;
                ferryG.transform.Translate(0, 0, -move);
                ridenG += move;
                if (ferryChoice == 2)
                    player.transform.Translate(0, 0, move);
            }
            else if (ferryChoice == 2)
                FeedbackTime = true;
        }
    }


    private void HandleFeedback()
    {
        FeedbackTime = FerryCalled = FerryRidingR = FerryRidingG = false;
        if (GameOptions.requestFeedback == RequestFeedback.Voluntary)
        {
            FeedbackDevice.SetActive(true);
            feedbackChoice.SetActive(true);
            FeebackChoiceButton1.SetActive(true);
            FeedbackChoiceButton2.SetActive(true);
            FeedbackWait.SetActive(false);
        }
        else if (RequestPlayerFeedback()) // require player to send feedback
        {
            FeedbackSubmitted = false;
            feedbackChoice.SetActive(true);
            FeebackChoiceButton1.SetActive(false);
            FeedbackChoiceButton2.SetActive(false);
            FeedbackWait.SetActive(true);
            SendFeedback(); // inform the system that a decision to send feedback was made
            controller.enabled = false;
            FeedbackDevice.SetActive(true);
        }
        else // no feedback, keep playing\
        {
            DeclineFeedback();
            FeedbackSubmitted = true;
        }
        ferryR.SetActive(false);
        ferryG.SetActive(false);
    }


    public void MoveFerry()
    {
        SetDashboard();
        if (GameOptions.DynamicFerryIdentity)
            SetFerryColors();
        controller.enabled = false; // player lose control on movement
        FerryChoiceTime = true;
    }


    public void ScoreUp()
    {
        Score++;
        CoinsCount.text = "Coins: " + Score.ToString() + "  Island " + (trial + trialDecrement) + " out of 30";
        if (clearCoins)
        {
            answer.coins.Clear();
            clearCoins = false;
        }  
        answer.coins.Add(time_game);
    }

    private void MoveObjects() // move trees and coins to the new island
    {
        float x = (float)-34.5;
        player.SetActive(false);
        wall.transform.Translate(0, 0, x);
        obstracles.transform.Translate(0, 0, x);
        Bridges.transform.Translate(0, 0, x);
        for (int i = 0; i < 15; i++)
        {
            Coins[i].SetActive(false);
            Coins[i].transform.Translate(0, 0, x);
        }
        player.SetActive(true);

        for (int i = 0; i < 15; i++)
            Coins[i].SetActive(true);

        float sl;
        for (int i = 0; i < 9; i++)
        {
            sl = UnityEngine.Random.Range((float)0.02, (float)0.4);
            Trees[i].transform.Translate(0, 0, x);
            Trees[i].transform.localScale = new Vector3(sl, sl, (float)0.003 * 5 * sl);
        }
    }

    // This method is called when user click the ferry button
    public void OnFerryCall(int ferry_called)
    {
        ferryChoice = ferry_called;
        switch (ferryChoice)
        {
            case 0:
                activeLight = LightR;
                LightR.SetActive(true);
                LightG.SetActive(false);
                break;
            case 2:
                activeLight = LightG;
                LightR.SetActive(false);
                LightG.SetActive(true);

                break;
        }
        // Set and save the riding  speed based on ferry choice
        double speed = -1;
        switch (ferryChoice)
        {
            case 0:
                speed = ferryRspeed;
                break;
            case 2:
                speed = ferryGspeed;
                break;
        }
        answer.ferrySpeed = (int)(100.0f * speed);
        RedFerryButton.SetActive(false);
        GreenFerryButton.SetActive(false);
        Dashboard.SetActive(false);
        FerrySign.SetActive(true);
        FerryCalled = true;
    }



    public void OnFeedbackButtonDown(bool direction)
    {
        if (direction)
            FeedbackButtonHighPressed = true;
        else
            FeedbackButtonLowPressed = true;
    }



    public void OnFeedbackButtonUp(bool direction)
    {
        if (direction)
            FeedbackButtonHighPressed = false;
        else
            FeedbackButtonLowPressed = false;
    }



    public void OnSubmit()
    {
        FeedbackSubmitted = true;
        FeedbackDevice.SetActive(false);
        answer.FeedbackScore = (int)FeedbackSlider.value;
        answer.timeElapsed = time_game;
        answer.ferryChoice = ferryChoice;
        string answerJson = JsonUtility.ToJson(answer);
        string metadataJson = JsonUtility.ToJson(metadata);
        if (GameOptions.Standalone == false) // experiment mode
        {
            StartCoroutine(WebRequestManager1.instance.SubmitPage(answerJson, metadataJson));
        }
        clearCoins = true;
    }




    private void SetDashboard()
    {
        // We need to set ferry speed at this time for the dashboard display
        SetFerrySpeed(1.5); // 1.5 is a speed factor we use to calibrate speed range
        // default when present dash == 3, accurate dashboard
        DashboardRedFerrySlider.value = (18.5f * (float)ferryRspeed - 0.6f) * (float)GameOptions.dashboardSNR + ((1.0f - (float)GameOptions.dashboardSNR) * (float)UnityEngine.Random.Range(0, 100));
        DashboardGreenFerrySlider.value = (18.5f * (float)ferryGspeed - 0.6f) * (float)GameOptions.dashboardSNR + ((1.0f - (float)GameOptions.dashboardSNR) * (float)UnityEngine.Random.Range(0, 100));
        // save dashboards displays:
        answer.dashboard1Value = (int)DashboardRedFerrySlider.value;
        answer.dashboard2Value = (int)DashboardGreenFerrySlider.value;
    }


    private void SetFerrySpeed(double speedFactor)
    {
        if ((GameOptions.speedAssignment == SpeedAssignment.RandomelyAssignSpeed && UnityEngine.Random.Range(0, 2) == 0) || GameOptions.speedAssignment == SpeedAssignment.LeftFerryFaster) // left ferry faster
        {
            ferryRspeed = UnityEngine.Random.Range(GameOptions.FastFerryMinSpeed, GameOptions.FastFerryTopSpeed); // Red ferry, on the left side, faster
            ferryGspeed = UnityEngine.Random.Range(GameOptions.SlowFerryMinSpeed, GameOptions.SlowFerryTopSpeed); // Green ferry on the right side, slower
        }
        else
        {
            ferryGspeed = UnityEngine.Random.Range(GameOptions.FastFerryMinSpeed, GameOptions.FastFerryTopSpeed);
            ferryRspeed = UnityEngine.Random.Range(GameOptions.SlowFerryMinSpeed, GameOptions.SlowFerryTopSpeed);
        }
        speedR = ferryRspeed / speedFactor;
        speedG = ferryGspeed / speedFactor;
    }


    public void FinishGame()
    {
        WebRequestManager1.instance.SubmitMessage("finished game naturally");
        if (GameActive)
        {
            GameActive = false;
            int FinalScore = (int)Score + 1;
            SaveGameScore(FinalScore.ToString());
        }
    }

    public void SaveGameScore(string score)
    {
        //  Debug.Log("Reached save game score");Debug.Log(score);
        const string URL = Constants.URLBASE + "/api/gamescore";
        PostRequest reqParams = new PostRequest(Constants.SCORESUBMIT_OPCODE, URL);
        reqParams.form.AddField("score", score);
        StartCoroutine(WebRequestManager1.instance.sendData(reqParams));
    }

    public void SendFeedback()
    {
        answer.FeedbackDelivered = true;
        FeebackChoiceButton1.SetActive(false);
        FeedbackChoiceButton2.SetActive(false);
        FeedbackWait.SetActive(true);
        _waitNow = true;
        //feedbackChoice.SetActive(false);
    }

    public void DeclineFeedback()
    {
        answer.FeedbackDelivered = false;
        answer.FeedbackScore = -1;
        OnSubmit();
    }

    void setSliderType(int type)
    {
        switch (type)
        {
            case 0: // clickbar
                sliderbuttonUP.SetActive(false); sliderbuttonDown.SetActive(false); // buttons disabled
                FeedbackSlider.interactable = true;// slider is interactable
                break;
            case 1: // slider
                sliderbuttonUP.SetActive(true); sliderbuttonDown.SetActive(true);// buttons enabled
                FeedbackSlider.interactable = false; // slider is NOT interactable
                break;

        }
    }



    public void SetFerryColors()
    {
        Color clr = new Color(1, 0, 0), clr2 = new Color(1, 0, 0);
        string tx1 = "", tx2 = "";

        pollR.GetComponent<Renderer>().material.color = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f)); //(1f, 0.35f, 0f);
        pollG.GetComponent<Renderer>().material.color = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f)); //(1f, 0.35f, 0f);

        switch (trial % 10)
        {
            case 1:
                clr = new Color(1, 0, 0); clr2 = new Color(0, 1, 0); tx1 = "Red"; tx2 = "Green";
                break;
            case 2:
                clr = new Color(0, 0, 1); clr2 = new Color(1, 1, 0); tx1 = "Blue"; tx2 = "Yellow";
                break;
            case 3:
                clr = new Color(0, 1, 1); clr2 = new Color(1, 0, 1); tx1 = "Sky"; tx2 = "Purple";
                break;
            case 4:
                clr = new Color(1, 0.5f, 0); clr2 = new Color(0.5f, 1, 0); tx1 = "Orange"; tx2 = "Lime";
                break;
            case 5:
                clr = new Color(0.5f, 0.2f, 1); clr2 = new Color(1, 1, 0.5f); tx1 = "Gulu"; tx2 = "Cream";
                break;
            case 6:
                clr = new Color(0.5f, 1, 0.8f); clr2 = new Color(1, 0.5f, 1); tx1 = "Mushroom"; tx2 = "Pink";
                break;
            case 7:
                clr = new Color(0.25f, 0.5f, 0); clr2 = new Color(0.5f, 0.25f, 0); tx1 = "Mustard"; tx2 = "Coffee";
                break;
            case 8:
                clr = new Color(0.5f, 0.1f, 0.35f); clr2 = new Color(1, 0.25f, 0.5f); tx1 = "Deep"; tx2 = "Rock";
                break;
            case 9:
                clr = new Color(0.25f, 0.3f, 0.1f); clr2 = new Color(0.15f, 0.25f, 1); tx1 = "Dark"; tx2 = "Moon";
                break;
            case 0:
                clr = new Color(1, 1, 0.5f); clr2 = new Color(0.5f, 0.6f, 0.7f); tx1 = "Bazuka"; tx2 = "Gray";
                break;
            default:
                ferryR.GetComponent<Renderer>().material.color = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f)); //(1f, 0.35f, 0f);
                ferryG.GetComponent<Renderer>().material.color = new Color(UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f), UnityEngine.Random.Range(0.0f, 1.0f)); //(1f, 0.35f, 0f);

                break;
        }

        clr.a = 0.5f; clr2.a = 0.5f;
        ferryR.GetComponent<Renderer>().material.color = RedFerryButton.GetComponent<Image>().color = clr;
        ferryG.GetComponent<Renderer>().material.color = GreenFerryButton.GetComponent<Image>().color = clr2;
        Vector3 newScale = ferryR.transform.localScale;
        newScale.x = UnityEngine.Random.Range(0.5f, 1.5f);
        ferryR.transform.localScale = newScale;
        newScale.x = UnityEngine.Random.Range(0.5f, 1.5f);
        ferryG.transform.localScale = newScale;
        RedFerryButton.GetComponentInChildren<Text>().text = tx1 + " Ferry";
        GreenFerryButton.GetComponentInChildren<Text>().text = tx2 + " Ferry";
    }

    public void TutorialNext(int page)
    {
        if (page == 3)
        {
            SessionTimer = true;
            SessionTimertime = UnityEngine.Random.Range(5, 12);
        }

        if (page == 5)
            controller.enabled = true;
        Slides[page].SetActive(false);
    }

   

    bool RequestPlayerFeedback()
    {
        int FeedbackRequestRate = 0;
        switch (GameOptions.requestFeedback)
        {
            case RequestFeedback.Always:
                FeedbackRequestRate = 100;
                break;
            case RequestFeedback.Request75Percent:
                FeedbackRequestRate = 75;
                break;
            case RequestFeedback.Request50Percent:
                FeedbackRequestRate = 50;
                break;
            case RequestFeedback.Request25Percent:
                FeedbackRequestRate = 25;
                break;
            case RequestFeedback.Never:
                FeedbackRequestRate = 0;
                break;
        }
        int i = UnityEngine.Random.Range(1, 100);
        if (i < FeedbackRequestRate)
            return true;
        return false;
    }



    bool ShowPlayerDashboardInfo()
    {
        int DashboardInfo = 0;
        switch (GameOptions.showDashboard)
        {
            case ShowDashboard.Always:
                DashboardInfo = 100;
                break;
            case ShowDashboard.Show75Percent:
                DashboardInfo = 75;
                break;
            case ShowDashboard.Show50Percent:
                DashboardInfo = 50;
                break;
            case ShowDashboard.Show25Percent:
                DashboardInfo = 25;
                break;
            case ShowDashboard.Never:
                DashboardInfo = 0;
                break;
        }
        if (UnityEngine.Random.Range(1, 100) < DashboardInfo)
            return true;
        return false;
    }




    // Once we start the game we set the game options 
    public void SetGameOptions()
    {
        SettingsPage.SetActive(false);

        // Place player in the selected worlds
        GameOptions.world = (VirtualWorld)DDWorld.value;
        switch (GameOptions.world)
        {
            case VirtualWorld.YellowIslands:
                player.transform.localPosition = new Vector3(30f, 1f, 5f);
                WorldInfo.text = "Welcome to the Yellow Islands!";
                break;
            case VirtualWorld.GreenIslands:
                player.transform.localPosition = new Vector3(0f, 1f, 5f);
                WorldInfo.text = "Welcome to the Green Islands!";
                break;
            case VirtualWorld.RedIslands:
                player.transform.localPosition = new Vector3(-30f, 1f, 5f);
                WorldInfo.text = "Welcome to the Red Islands!";
                break;
        }


        // Set dashboard display
        GameOptions.showDashboard = (ShowDashboard)DDShowDashboard.value;


        // Set slider type
        GameOptions.ratingDevice = (RatingDevice)DDRatingDevice.value;
        SetRatingDevice();

        // Request feeback
        GameOptions.requestFeedback = (RequestFeedback)DDRequestFeedback.value;


        // Set dashboard info type (trends or current)
        GameOptions.dashboardType = (DashboardType)DDDashboardType.value;
        switch (GameOptions.dashboardType)
        {
            case DashboardType.CurrentRatings:
                ShowRatingTrends.SetActive(false);
                ShowCurrentRatings.SetActive(true);
                break;
            case DashboardType.Trends:
                ShowRatingTrends.SetActive(true);
                ShowCurrentRatings.SetActive(false);
                break;
        }

        // Set number of ferries
        if (DDNumFerries.value == 0)
            GameOptions.TwoFerries = false;
        else
            GameOptions.TwoFerries = true;


        if (DDDynamicFerryIdentity.value == 0)
            GameOptions.DynamicFerryIdentity = false;
        else
            GameOptions.DynamicFerryIdentity = true;

        // Set ferrys speed range 
        GameOptions.FastFerryTopSpeed = SLFastFerryTopSpeed.value;
        GameOptions.FastFerryMinSpeed = SLFastFerryMinSpeed.value;
        GameOptions.SlowFerryTopSpeed = SLSlowFerryTopSpeed.value;
        GameOptions.SlowFerryMinSpeed = SLSlowFerryMinSpeed.value;

        // Set ferry speed assignment 
        GameOptions.speedAssignment = (SpeedAssignment)DDFerrySpeedAssignment.value;

        // Set Dashboard SNR
        // GameOptions.dashboardSNR = (DashboardSNR)DDDashboardSNR.value;
        switch (DDDashboardSNR.value)
        {
            case 0:
                GameOptions.dashboardSNR = 1;
                break;
            case 1:
                GameOptions.dashboardSNR = 0.5;
                break;
            case 2:
                GameOptions.dashboardSNR = 0;
                break;
        }

        // Set trend resolution 
        GameOptions.trendResolution = SLTrendResolution.value;
        TrendGraph1.GetComponent<Graphic>().interval = (float)GameOptions.trendResolution;
        TrendGraph2.GetComponent<Graphic>().interval = (float)GameOptions.trendResolution;

        // Set rating delay & slider friction
        GameOptions.ratingDelay = SLRatingDelay.value;
        GameOptions.sliderFriction = SLSliderFriction.value;

        // show birdge
        GameOptions.allowBridge = (AllowBridge)DDPresentBridge.value;
        ShowBridges();



        controller.enabled = true;
    }


    public void AfterPageSubmitted()
    {
        if (GameOptions.Standalone == false) // experiment mode
        {
             StartCoroutine(WebRequestManager1.instance.GetPage()); // we now get the information from the new page
        }

    }


    void SetRatingDevice()
    {
        switch (GameOptions.ratingDevice)
        {
            case RatingDevice.Clickbar:
                setSliderType(0); // clickbar
                break;
            case RatingDevice.Slider:
                setSliderType(1); // costly slider
                break;
        }
    }

    void ShowBridges()
    {
        //GameOptions.allowBridge == AllowBridge.Always || GameOptions.allowBridge == AllowBridge.YellowIslands)
        //               Bridges.SetActive(true);
        Bridges.SetActive(false);
        switch (GameOptions.allowBridge)
        {
            case AllowBridge.Always:
                Bridges.SetActive(true);
                break;
            case AllowBridge.YellowIslands:
                if (GameOptions.world == VirtualWorld.YellowIslands)
                    Bridges.SetActive(true);
                break;
            case AllowBridge.GreenIslands:
                if (GameOptions.world == VirtualWorld.GreenIslands)
                    Bridges.SetActive(true);
                break;
            case AllowBridge.RedIslands:
                if (GameOptions.world == VirtualWorld.RedIslands)
                    Bridges.SetActive(true);
                break;

        }
    }


    // This function is called after Psynet gave us the game parameters, which are in jsonData
    public void AfterPageUpdate(string jsonData)
    {
        jsonData = jsonData.Replace("\"{", "{").Replace("}\"", "}").Replace("\\", "");
        dashboardJson = JsonUtility.FromJson<DashboardJson>(jsonData);

        if (dashboardJson.attributes.type == "UnityQuestionPage")// This is for the within Unity Question page
        {
            questionJson = JsonUtility.FromJson<QuestionJson>(jsonData);
            SelectWorld();
            return;
        }

            // Set dashboard rate
            switch (dashboardJson.contents.dashboard_rate)
        {
            case 0:
                GameOptions.showDashboard = ShowDashboard.Never;
                break;
            case 25:
                GameOptions.showDashboard = ShowDashboard.Show25Percent;
                break;
            case 50:
                GameOptions.showDashboard = ShowDashboard.Show50Percent;
                break;
            case 75:
                GameOptions.showDashboard = ShowDashboard.Show75Percent;
                break;
            case 100:
                GameOptions.showDashboard = ShowDashboard.Always;
                break;
            default:
                Debug.Log("Error: dashboard rate must be 0, 25, 50, 75 or 100");
                break;
        }


        // set feedback rate:
        switch (dashboardJson.contents.feedback_rate)
        {
            case -1:
                GameOptions.requestFeedback = RequestFeedback.Voluntary;
                break;
            case 0:
                GameOptions.requestFeedback = RequestFeedback.Never;
                break;
            case 25:
                GameOptions.requestFeedback = RequestFeedback.Request25Percent;
                break;
            case 50:
                GameOptions.requestFeedback = RequestFeedback.Request50Percent;
                break;
            case 75:
                GameOptions.requestFeedback = RequestFeedback.Request75Percent;
                break;
            case 100:
                GameOptions.requestFeedback = RequestFeedback.Always;
                break;
            default:
                Debug.Log("Error: feedback rate values must be -1, 0, 25, 50, 75 or 100");
                break;
        }



        Debug.Log("dashboard is now " + dashboardJson.contents.dashboard_rate);

        // Now set location:

        if (current_world == -1 || current_world != dashboardJson.contents.island_color)
        {
            current_world = dashboardJson.contents.island_color;
            Bridges.SetActive(false);
            switch (dashboardJson.contents.island_color)
            {
                case 0:
                    player.transform.localPosition = new Vector3(30f, 1f, 5f);
                    WorldInfo.text = "Welcome to the Yellow Islands!";
                    if (GameOptions.allowBridge == AllowBridge.Always || GameOptions.allowBridge == AllowBridge.YellowIslands)
                        Bridges.SetActive(true);
                    break;
                case 1:
                    player.transform.localPosition = new Vector3(0f, 1f, 5f);
                    WorldInfo.text = "Welcome to the Green Islands!";
                    if (GameOptions.allowBridge == AllowBridge.Always || GameOptions.allowBridge == AllowBridge.GreenIslands)
                        Bridges.SetActive(true);
                    break;
                case 2:
                    player.transform.localPosition = new Vector3(-30f, 1f, 5f);
                    WorldInfo.text = "Welcome to the Red Islands!";
                    if (GameOptions.allowBridge == AllowBridge.Always || GameOptions.allowBridge == AllowBridge.RedIslands)
                        Bridges.SetActive(true);
                    break;
            }
            ResetObjectsLocation();
            Debug.Log("World is now " + dashboardJson.contents.island_color);
            // Set bridges

            if (GameOptions.showDashboard == ShowDashboard.Never)//DashboardInfo == 0)
            {
                WorldInfo.text = WorldInfo.text + " Here you cross the water on your own.";
                // Bridges.SetActive(true);
            }

            else if (GameOptions.showDashboard == ShowDashboard.Always)//DashboardInfo == 100)
            {
                // Bridges.SetActive(false);
                WorldInfo.text = WorldInfo.text + " Here you rate the ferry and get rating information about ferries.";
            }

            else
            {
                //  Bridges.SetActive(false);
                WorldInfo.text = WorldInfo.text + " Here you occationally rate the ferry and get some rating information.";
            }
            setWorldChoices(current_world);
            controller.enabled = false; // player lose control on movement
            SessionTimer = true;
            SessionTimertime = UnityEngine.Random.Range(8, 15);
            Slides[5].SetActive(true);

        }
    }


    void ResetObjectsLocation()
    {
        wall.transform.localPosition = objectsDefaultLocations[0];
        for (int i = 1; i < 10; i++)
        {
            Trees[i - 1].transform.localPosition = objectsDefaultLocations[i];
        }
        for (int i = 10; i < 25; i++)
        {
            Coins[i - 10].transform.localPosition = objectsDefaultLocations[i];
        }
        Bridges.transform.localPosition = objectsDefaultLocations[25];
    }

    void GetObjectsLocation()
    {
        objectsDefaultLocations[0] = wall.transform.localPosition;
        for (int i = 1; i < 10; i++)
        {
            objectsDefaultLocations[i] = Trees[i - 1].transform.localPosition;
        }
        for (int i = 10; i < 25; i++)
        {
            objectsDefaultLocations[i] = Coins[i - 10].transform.localPosition;
        }
        objectsDefaultLocations[25] = Bridges.transform.localPosition;
    }

    public void getPsynetPage() // just for testing
    {
        //  StartCoroutine(WebRequestManager.instance.GetPage());
    }

    public void trendplot()
    {
        TrendmeObj.GetComponent<trendme>().AddScore(answer.dashboard2Value / 20);
        TrendmeObj2.GetComponent<trendme>().AddScore(answer.dashboard1Value / 20);
    }


    public void ShowFerryOptions(bool show)
    {
        FerryOptions.SetActive(show);
        DashboardOptions.SetActive(false);
        RatingsOptions.SetActive(false);
        CoinsOptions.SetActive(false);
    }


    public void ShowDashboardOptions(bool show)
    {
        DashboardOptions.SetActive(show);
        FerryOptions.SetActive(false);
        RatingsOptions.SetActive(false);
        CoinsOptions.SetActive(false);
    }

    public void ShowRatingsOptions(bool show)
    {
        RatingsOptions.SetActive(show);
        DashboardOptions.SetActive(false);
        FerryOptions.SetActive(false);
        CoinsOptions.SetActive(false);
    }

    public void ShowCoinsOptions(bool show)
    {
        CoinsOptions.SetActive(show);
        RatingsOptions.SetActive(false);
        DashboardOptions.SetActive(false);
        FerryOptions.SetActive(false);
    }


    void setWorldChoices(int i)
    {
        switch(worldIndex)
        {
            case 0:
                WebRequestManager1.instance.WorldColor1 = i;
                WebRequestManager1.instance.WorldFeedbackRate1 = FeedbackRateToInt(GameOptions.requestFeedback);
                WebRequestManager1.instance.WorldDashboardRate1 = DashRateToInt(GameOptions.showDashboard);
                break;
            case 1:
                WebRequestManager1.instance.WorldColor2 = i;
                WebRequestManager1.instance.WorldFeedbackRate2 = FeedbackRateToInt(GameOptions.requestFeedback);
                WebRequestManager1.instance.WorldDashboardRate2 = DashRateToInt(GameOptions.showDashboard);
                break;
            case 2:
                WebRequestManager1.instance.WorldColor3 = i;
                WebRequestManager1.instance.WorldFeedbackRate3 = FeedbackRateToInt(GameOptions.requestFeedback);
                WebRequestManager1.instance.WorldDashboardRate3 = DashRateToInt(GameOptions.showDashboard);
                break;

        }
        worldIndex++;
    }


    void FeedbackRateToInt()
    {

    }

    int DashRateToInt(ShowDashboard d)
    {
        switch(d)
        {
            case ShowDashboard.Never:
                    return (0);
            case ShowDashboard.Show25Percent:
                return (25);
            case ShowDashboard.Show50Percent:
                return (50);
            case ShowDashboard.Show75Percent:
                return (75);
            case ShowDashboard.Always:
                return (100);
        }
        return (-1);
    }

    int FeedbackRateToInt(RequestFeedback d)
    {
        switch (d)
        {
            case RequestFeedback.Never:
                return (0);
            case RequestFeedback.Request25Percent:
                return (25);
            case RequestFeedback.Request50Percent:
                return (50);
            case RequestFeedback.Request75Percent:
                return (75);
            case RequestFeedback.Always:
                return (100);
        }
        return (-1);
    }


    public void SelectWorld()
    {
        current_world = -1; // force rest to the new world next time we call 
        controller.enabled = false; // player lose control on movement
        // set the button colors based on worlds presented earlier
        Color32 clr1 = SetColor(WebRequestManager1.instance.WorldColor1);
        Color clr2 = SetColor(WebRequestManager1.instance.WorldColor2);
        Color clr3 = SetColor(WebRequestManager1.instance.WorldColor3);
        WorldOption1.GetComponent<Image>().color = clr1;
        WorldOption2.GetComponent<Image>().color = clr2;
        WorldOption3.GetComponent<Image>().color = clr3;
        // Set button text:
        WorldOption1.GetComponentInChildren<Text>().text = SetButtonText(WebRequestManager1.instance.WorldColor1, WebRequestManager1.instance.WorldFeedbackRate1);
        WorldOption2.GetComponentInChildren<Text>().text = SetButtonText(WebRequestManager1.instance.WorldColor2, WebRequestManager1.instance.WorldFeedbackRate2);
        WorldOption3.GetComponentInChildren<Text>().text = SetButtonText(WebRequestManager1.instance.WorldColor3, WebRequestManager1.instance.WorldFeedbackRate3);


        // Dalton Experiment
        chooseWorldBtn1.gameObject.SetActive(true);
        chooseWorldBtn2.gameObject.SetActive(true);
        if (AssignWorld)
        {
            WebRequestManager1.instance.SubmitMessage("Assigned world");
            SelectWorldText.text = "We randomly assigned you to play in a world:";

            chooseWorldBtn1.gameObject.SetActive(assignedWorldOrder[WorldAssignmentIndex]);
            chooseWorldBtn2.gameObject.SetActive(!assignedWorldOrder[WorldAssignmentIndex]);
            WorldAssignmentIndex++;
        }
        SelectTheWorld.SetActive(true);
       // StartCoroutine(WebRequestManager.instance.SubmitPage("0", JsonUtility.ToJson(metadata)));
    }




    public void OnSelectWord(int i) // should be World ;-) 
    {
        string str;
        dashboardJson.attributes.type = "";
        controller.enabled = true; // player lose control on movement
        SelectTheWorld.SetActive(false);
        switch (i)
        {
            case 1:
                
                {
                    dashboardJson.contents.island_color = WebRequestManager1.instance.WorldColor1;
                    dashboardJson.contents.dashboard_rate = WebRequestManager1.instance.WorldDashboardRate1;
                    dashboardJson.contents.feedback_rate = WebRequestManager1.instance.WorldFeedbackRate1;
                    dashboardJson.attributes.type = "";
                    str = JsonUtility.ToJson(dashboardJson);
                    AfterPageUpdate(str);
                }
                break;

            case 2:
                
                {
                    dashboardJson.contents.island_color = WebRequestManager1.instance.WorldColor2;
                    dashboardJson.contents.dashboard_rate = WebRequestManager1.instance.WorldDashboardRate2;
                    dashboardJson.contents.feedback_rate = WebRequestManager1.instance.WorldFeedbackRate2;
                    dashboardJson.attributes.type = "";
                    str = JsonUtility.ToJson(dashboardJson);
                    AfterPageUpdate(str);
                }
                break;

            case 3:
                
                {
                    dashboardJson.contents.island_color = WebRequestManager1.instance.WorldColor3;
                    dashboardJson.contents.dashboard_rate = WebRequestManager1.instance.WorldDashboardRate3;
                    dashboardJson.contents.feedback_rate = WebRequestManager1.instance.WorldFeedbackRate3;
                    dashboardJson.attributes.type = "";
                    str = JsonUtility.ToJson(dashboardJson);
                    AfterPageUpdate(str);
                }
                break;
        }
        str = "Selected world cooperation rate = " + dashboardJson.contents.feedback_rate;
        WebRequestManager1.instance.SubmitMessage(str);
    }

    Color32 SetColor(int i) // yellow, green, red
    {
        switch (i)
        {
            case 0:
                return (new Color32(210, 210, 25, 230)); // yellow
            case 1:
                return (new Color32(25, 210, 40, 255)); // green
            default:
                return (new Color32(224, 110, 100, 255)); // red
        }
    }

    string SetButtonText(int islandcolor, int cooperation)
    {
        string str = "";

        switch (islandcolor)
        {
            case 0:
                str = "Yellow Islands:\n";
                break;
            case 1:
                str = "Green Islands:\n";
                break;
            default:
                str = "Red Islands:\n";
                break;
        }

        switch (cooperation)
        {
            case 0:
                str = str + "Here you do not rate the ferry";// cross the lake on your own";
                break;
            case 50:
                str = str + "Here you choose when to rate the ferry"; //occationally rate the ferry and get some rating information";
                break;
            case 100:
                str = str + "Here you rate the ferry";// and get rating information about ferries";
                break;
            case -1:
                str = str + "Here you rate the ferry";// and get rating information about ferries";
                break;
        }
        return (str);
    }



    public void OpenTheBridge()
    {
        // set call ferry active to false
        wall.SetActive(false);
        // set no reverse and slow speed
        controller.movementSettings.ForwardSpeed = 2;
        controller.movementSettings.BackwardSpeed = 0;

    }
    public void CloseTheBridge()
    {
        MoveObjects();// move trees and coins to the new island
        // move objects -- including self!
        OnSubmit(); // send info and continue playing
        wall.SetActive(true);
        // regain normal player speed
        controller.movementSettings.ForwardSpeed = 5;
        controller.movementSettings.BackwardSpeed = 2;
        WebRequestManager1.instance.SubmitMessage("Crossed bridge");
    }

   


    public void RatingToggleChanged()
    {
        if(RatingToggle.isOn)
        {
            GameOptions.showDashboard = ShowDashboard.Always;
            NoRatings1.SetActive(false);
            NoRatings2.SetActive(false);
            GameOptions.ratingDelay -= 1.0;
            GameOptions.requestFeedback = RequestFeedback.Always;
            WebRequestManager1.instance.SubmitMessage("Ratings turned on: " + trial + "Rating dalay:" + GameOptions.ratingDelay);
          
        }
        else
        {
            GameOptions.showDashboard = ShowDashboard.Never;
            NoRatings1.SetActive(true);
            NoRatings2.SetActive(true);
            GameOptions.requestFeedback = RequestFeedback.Never;
            WebRequestManager1.instance.SubmitMessage("Ratings turned off: " + trial + "Rating dalay:" + GameOptions.ratingDelay);
        }
        RatingToggle.interactable = false;
    }

    

}
