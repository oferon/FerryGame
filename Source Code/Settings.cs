using System;
using System.Collections;
using System.Collections.Generic;



// Settings class
[Serializable]
public class Settings // change name to MetaData
{
    public bool Standalone = true; // true: game logic is driven by setting page (design); false: game game logic driven by Psynet or other engine (experiment)
    public VirtualWorld world = VirtualWorld.GreenIslands;
    public bool TwoFerries = true;
    public bool DynamicFerryIdentity = true;
    public float FastFerryTopSpeed=6.0f, FastFerryMinSpeed=3.0f, SlowFerryTopSpeed=2.5f, SlowFerryMinSpeed=1.3f;
    public SpeedAssignment speedAssignment = SpeedAssignment.RandomelyAssignSpeed;
    public ShowDashboard showDashboard = ShowDashboard.Always;
    public RatingDevice ratingDevice = RatingDevice.Slider;
    public RequestFeedback requestFeedback = RequestFeedback.Always;
    public DashboardType dashboardType = DashboardType.CurrentRatings;
    public AllowBridge allowBridge = AllowBridge.Never;
    public double dashboardSNR = 1.0, trendResolution, ratingDelay = 1, sliderFriction = 0.1;
    public int CoinsPerIsland = 5;
    public bool PresentObstacles = false;
    public bool VanishingCoins = false;
    public float VanishingCoinRate = 0;
}


// This is the 3 class structure that correspond to the json the Psynet send with game parameters
[Serializable]
public class Uattributes
{
    public int session_id = -1;
    public string type = "nothing";
}


[Serializable] // this will parse the json from psynet, tell unity the parameters of the game: how much feedback to request, and how much dashboard information to show.
// This is currently called in start() but we will also want to call it might also change during game
public class Ucontents//DashboardJson
{
    public int dashboard_rate = 0; // dashboard_rate":dashboard_rate, "feedback_rate":feedback_rate, "island_color":my_perm[mtype]}
    public int feedback_rate = 0;
    public int island_color = 0;
}

[Serializable]
public class Qcontents//DashboardJson
{
    public string question = "";
    public List<string> options;   
}


[Serializable]
public class DashboardJson
{
    public Uattributes attributes;
    public Ucontents contents;
}


[Serializable]
public class QuestionJson
{
    public Uattributes attributes;
    public Qcontents contents;
}


[Serializable] // this will become the json we send to psynet as infos, to document what happaned in each game stage: coins collected, dashboard diplay, ferry choice & speed, feedback
public class Answer // change name to MetaData
{
    public List<double> coins = new List<double>();
    public double timeElapsed = 0;
    public bool showDashboard1, showDashboard2;
    public int dashboard1Value, dashboard2Value;
    public int ferrySpeed, ferryChoice;
    public bool FeedbackDelivered = false;
    public int FeedbackScore = 0;
}

[Serializable] // Metadata be to sent to PsyNet
public class Metadata
{
    public string timestamp;
}


// Setting variable enums
public enum VirtualWorld
{
    YellowIslands,
    GreenIslands,
    RedIslands
}

public enum SpeedAssignment
{
    LeftFerryFaster,
    RightFerryFaster,
    RandomelyAssignSpeed
}

public enum RatingDevice
{
    Clickbar,
    Slider
}

public enum DashboardType
{
    CurrentRatings,
    Trends
}

public enum ShowDashboard
{
    Always,
    Show75Percent,
    Show50Percent,
    Show25Percent,
    Never
}

public enum RequestFeedback
{
    Always,
    Request75Percent,
    Request50Percent,
    Request25Percent,
    Never,
    Voluntary
}

public enum AllowBridge
{
    Always,
    YellowIslands,
    GreenIslands,
    RedIslands,
    Never
}


public class Constants
{

    public const string URLBASE = "http://u1.u311.org";
    public const int SCORESUBMIT_OPCODE = 0x100;
    public const int TheRedFerry = 0;
    public const int TheYellowFerry = 1;
    public const int TheGreenFerry = 2;
}
