using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * 		requestParams["utmac"]  = // Account String UA-XXXXX
 * 		requestParams["utmhn"]  = // Hostname
		requestParams["utmdt"]  = // Page title - Possible use this as a level/map/section
		requestParams["utme"]   = // Event Parameters
		requestParams["utmt"]	= // Type of request: page, event, transaction, item, custom variable, default is page
		requestParams["utmp"]	= // Page request of the current page
		requestParams["utmcr"]  = // Language encoded for the browser
		requestParams["utmfl"]  = // flash version
		requestParams["utmn"]	= // Unique ID generated for each GIF request to prevent caching
		requestParams["utmsc"]	= // Screen color depth
		requestParams["utmsr"]	= // Screen resolution
		requestParams["utmwv"]  = // Tracking code version
		requestParams["utmul"]	= // Browser language http://www.metamodpro.com/browser-language-codes
		requestParams["utmcc"]  = // Cookie
 * 
 */

public class GoogleAnalytics : MonoBehaviour {
	
	public string propertyID;
	public string defaultURL;
	
	public static GoogleAnalytics instance;
	
	private Hashtable requestParams = new Hashtable();
	
	private string currentSessionStartTime;
	private string lastSessionStartTime;
	private string firstSessionStartTime;
	private int sessions;
	
	void Awake()
	{
		if(instance)
			DestroyImmediate(gameObject);
		else
		{
			DontDestroyOnLoad(gameObject);
			instance = this;
		}
	}
	
	public void Start(){
		//Get the player prefs last time played and current time
		currentSessionStartTime = GetEpochTime().ToString();
		lastSessionStartTime = SavedLastSessionStartTime;
		firstSessionStartTime = SavedFirstSessionStartTime;
		sessions = NumSessions;
		
		requestParams["utmac"] = propertyID;
		requestParams["utmhn"] = defaultURL;
		
		// Set the last session start time
		SavedLastSessionStartTime = currentSessionStartTime;
		// Increment the number of times played
		IncrSessions();
    }
	
	public void SetCustomVar(int index, string name, string value, int scope)
	{
		// optional scope values: 1 (visitor-level), 2 (session-level), or 3 (page-level).
		// https://developers.google.com/analytics/devguides/collection/gajs/gaTrackingCustomVariables	
		Debug.Log("Custom Var");
	}
	
	public void TrackLevel()
	{
		string levelName = Application.loadedLevelName;
		requestParams["utmt"] = GoogleTrackType.GALevel;
		requestParams["utmn"] = Random.Range(1000000000,2000000000).ToString();
		requestParams["utmcc"] = CookieData();
		requestParams["utmp"] = levelName;
		
		Dispatch();
		Clear();
	}
	
	public void TrackEvent(string category, string label, string action, int value)
	{
		requestParams["utmt"] = GoogleTrackType.GAEvent;
		requestParams["utmn"] = Random.Range(1000000000,2000000000).ToString();
		requestParams["utmcc"] = CookieData();
		
		Dispatch();
		Clear();
	}
	
	public void TrackTiming()
	{
	 	// https://developers.google.com/analytics/devguides/collection/gajs/gaTrackingTiming
		requestParams["utmt"] = GoogleTrackType.GATiming;
		requestParams["utmn"] = Random.Range(1000000000,2000000000).ToString();
		requestParams["utmcc"] = CookieData();
		
		Dispatch();
		Clear();
	}
	
	public void Dispatch()
	{
		// Send the data to the Google Servers
    	string urlParams = BuildRequestString();
		string url = "http://www.google-analytics.com/__utm.gif?" + urlParams;
		new WWW(url);
	}
	
	private void Clear()
	{
		// Remove so the slate it clean for new tracking
		requestParams.Remove("utmt");
		requestParams.Remove("utmn");
		requestParams.Remove("utmcc");
	}
	
	private long DeviceIdentifier
	{
        get{ return Hash (SystemInfo.deviceUniqueIdentifier ); }
	}
	
	private int NumSessions
	{
		get{ return PlayerPrefs.GetInt("gaNumSessions"); }
	}
	
	private void IncrSessions()
	{
		int sessions = PlayerPrefs.GetInt("gaNumSessions");
		sessions += 1;
		PlayerPrefs.SetInt("gaNumSessions", sessions);
	}
	
	private string SavedFirstSessionStartTime
	{
		get{ if (PlayerPrefs.HasKey("gaFirstSessionStartTime"))
				{
					return PlayerPrefs.GetString("gaFirstSessionStartTime");
				}else{
					long currentTime = GetEpochTime();
					PlayerPrefs.SetString("gaFirstSessionStartTime", currentTime.ToString());
					PlayerPrefs.SetString("gaLastSessionStartTime", currentTime.ToString());
					return PlayerPrefs.GetString("gaFirstSessionStartTime");
				}
			}	
	}
	
	private string SavedLastSessionStartTime
	{
		get{ return PlayerPrefs.GetString("gaLastSessionStartTime"); }
		set{ PlayerPrefs.SetString("gaLastSessionStartTime", value.ToString()); }
	}
	
	// Grab the cookie data for every event/pageview because it grabs the current time
	private string CookieData()
	{
		long currentTime  = GetEpochTime();
		long domainHash = Hash(defaultURL);
		
		// __utma Identifies unique Visitors
		string _utma   = domainHash + "." + DeviceIdentifier + "." + firstSessionStartTime + "." + 
			lastSessionStartTime + "." + currentSessionStartTime + "." + sessions + WWW.EscapeURL(";") + WWW.EscapeURL("+");

		// __utmz Referral information in the cookie
		string cookieUTMZstr = "utmcsr" + WWW.EscapeURL("=") + "(direct)" + WWW.EscapeURL("|") + 
			"utmccn" + WWW.EscapeURL("=") + "(direct)" + WWW.EscapeURL("|") + 
			"utmcmd" + WWW.EscapeURL("=") + "(none)" + WWW.EscapeURL(";");
		
		string _utmz = domainHash + "." + currentTime + "." + sessions + ".1." + cookieUTMZstr;
		
		return "__utma" + WWW.EscapeURL("=") + _utma + "__utmz" + WWW.EscapeURL("=") + _utmz;
	}
	
	private string BuildRequestString()
	{
		List<string> args = new List<string>();
		foreach( string key in requestParams.Keys ) {
			args.Add( key + "=" + requestParams[key] );	
		}
		return string.Join("&", args.ToArray());	
	}
	
	private long Hash(string url)
	{
		if(url.Length < 3) return Random.Range(10000000,99999999);
		
		int hash = 0;
		int hashCmp = 0;
		for(int urlLen=url.Length-1; urlLen>=0; urlLen--){
			int charCode = (int)url[urlLen];
            hash    = (hash<<6&268435455) + charCode + (charCode<<14);
            hashCmp = hash&266338304;
            hash    = hashCmp != 0 ? hash^hashCmp>>21 : hash;
		}
		return hash;
	}
	
	private long GetEpochTime() 
	{
		System.DateTime currentTime = System.DateTime.Now;
		System.DateTime epochStart  = System.Convert.ToDateTime("1/1/1970 0:00:00 AM");
		System.TimeSpan timeSpan    = currentTime.Subtract(epochStart);
		
		long epochTime = ((((((timeSpan.Days * 24) + timeSpan.Hours) * 60) + timeSpan.Minutes) * 60) + timeSpan.Seconds);
		
		return epochTime;
	}
}

public enum GoogleTrackType{
	GALevel,
	GAEvent,
	GATiming,
}