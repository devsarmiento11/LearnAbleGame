using System;
using System.Reflection;
namespace UnityEngine {
 public class MonoBehaviour { public bool isActiveAndEnabled=true; }
 public class GameObject { public bool activeSelf=true; public void SetActive(bool x){activeSelf=x;} }
 public class Sprite {}
 public class HeaderAttribute:Attribute {public HeaderAttribute(string s){} }
 public class TooltipAttribute:Attribute {public TooltipAttribute(string s){} }
 public class HideInInspector:Attribute {}
 public static class Debug {public static void Log(object x){} public static void LogError(object x){} }
 public static class Mathf {public static int RoundToInt(float x)=>(int)Math.Round(x);}
 public static class PlayerPrefs {public static void SetString(string a,string b){} public static void Save(){} }
}
namespace UnityEngine.UI {public class Image {public UnityEngine.GameObject gameObject=new(); public UnityEngine.Sprite sprite;} }
namespace TMPro {public class TMP_Text {public string text;} }
namespace UnityEngine.SceneManagement { public struct Scene {public string name=>"MathGame2Level1";} public static class SceneManager {public static string Loaded; public static Scene GetActiveScene()=>new(); public static void LoadScene(string s){Loaded=s;} } }
namespace SpeechToTextNamespace {public interface ISpeechToTextListener{} }
public static class ScoreManager {public static int CorrectLines,CurrentScore,Records; static bool recorded; public static void BeginActivity(){CorrectLines=CurrentScore=0; recorded=false;} public static void RecordActivity(string s,int n){if(!recorded){Records++; recorded=true;}} }
public static class SpeechToText {
 public enum Permission {Granted,Denied}
 public static bool Busy,Allowed=true,Available=true; public static int Starts,Cancels; public static Action<Permission> Callback;
 public static bool Initialize(string x)=>true; public static bool IsServiceAvailable()=>Available; public static bool IsBusy()=>Busy;
 public static bool CheckPermission()=>Allowed; public static void RequestPermissionAsync(Action<Permission> c){Callback=c;}
 public static bool Start(object x){Busy=true; Starts++;return true;} public static void ForceStop(){} public static void Cancel(){Busy=false; Cancels++;}
}
class Tests {
 static int count;
 static void Call(SpeechGameManager m,string name)=>typeof(SpeechGameManager).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(m,null);
 static void Check(bool ok,string label){if(!ok)throw new Exception(label); Console.WriteLine("PASS "+label);count++;}
 static SpeechGameManager New(){SpeechToText.Busy=false;SpeechToText.Allowed=true;SpeechToText.Available=true;UnityEngine.SceneManagement.SceneManager.Loaded=null; var m=new SpeechGameManager{result=new TMPro.TMP_Text(),missingNumbers=new[]{new SpeechGameManager.MissingNumber{correctNumber=6,numberImage=new UnityEngine.UI.Image(),numberSprite=new UnityEngine.Sprite()}}};Call(m,"Start");return m;}
 static void Main(){
 var m=New();Check(!m.missingNumbers[0].numberImage.gameObject.activeSelf,"answer hidden initially");
 m.OnResultReceived("seven",null);Check(!m.missingNumbers[0].solved && m.result.text=="Try Again","wrong answer stays hidden");
 m.OnResultReceived("sixteen",null);Check(!m.missingNumbers[0].solved,"sixteen is not six");
 m.OnResultReceived("six",null);Check(m.missingNumbers[0].solved && m.missingNumbers[0].numberImage.gameObject.activeSelf,"six reveals answer");
 m.Done();Check(ScoreManager.CurrentScore==100 && UnityEngine.SceneManagement.SceneManager.Loaded=="ScoreScene","correct score and success scene");
 int records=ScoreManager.Records;m.Done();Check(ScoreManager.Records==records,"Done cannot record twice");
 m=New();m.Done();Check(ScoreManager.CurrentScore==0 && UnityEngine.SceneManagement.SceneManager.Loaded=="ScoreSceneFailed" && ScoreManager.Records==records+1,"retry resets recording and unsolved level fails");
 m=New();m.OnResultReceived("6",null);Check(m.missingNumbers[0].solved,"numeric transcript accepted");
 m=New();m.OnResultReceived("six",7);Check(!m.missingNumbers[0].solved,"recognition error cannot solve");
 m=New();SpeechToText.Allowed=false;m.StartListening();m.StopListening();int starts=SpeechToText.Starts;SpeechToText.Callback(SpeechToText.Permission.Granted);Check(SpeechToText.Starts==starts,"permission granted after release does not start recording");
 m=New();m.StartListening();m.Done();Check(UnityEngine.SceneManagement.SceneManager.Loaded==null,"Done waits for final transcript");m.OnResultReceived("six",null);m.Done();Check(ScoreManager.CurrentScore==100,"final transcript included in score");
 m=New();m.StartListening();int cancels=SpeechToText.Cancels;m.isActiveAndEnabled=false;Call(m,"OnDisable");m.OnResultReceived("six",null);Check(SpeechToText.Cancels==cancels+1 && !m.missingNumbers[0].solved,"leaving cancels recognition and ignores late results");
 Console.WriteLine(count+" behavior checks passed (Unity and speech services simulated).");
 }
}
