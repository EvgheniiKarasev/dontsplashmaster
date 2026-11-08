using UnityEngine;

namespace DontSplash {
public sealed partial class SplashGame {
 bool confirmNewRun;
 bool HasSavedRun=>PlayerPrefs.GetInt("HasSave")==1;
 void SaveActiveRun(){if(mode!=Mode.Menu&&mode!=Mode.Win)Save();}
 void OnApplicationQuit(){SaveActiveRun();}
 void RestoreRunCheckpoint(){
  int index=PlayerPrefs.GetInt("DS.Checkpoint",-1);
  if(index>=0&&index<steps.Count){
   var step=steps[index];
   if(step.home.z>=EndZ-1&&step.kind==0){
    checkpointIndex=index;checkpointZ=step.home.z;spawn=step.home+Vector3.up*.55f;Respawn();
   }
  }
  int mask=PlayerPrefs.GetInt("DS.Valves");
  for(int i=0;i<valves.Count;i++)if((mask&(1<<i))!=0){valves[i].done=true;valves[i].charge=2.5f;valves[i].t.GetComponent<Renderer>().sharedMaterial=glow;}
 }
}
}
