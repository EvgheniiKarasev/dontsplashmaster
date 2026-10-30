using UnityEngine;
using UnityEngine.UI;
namespace DontSplash {
public sealed class TouchDisc : MaskableGraphic {
 public bool arrow;
 public Color rimColor=new Color(1,1,1,.9f);
 protected override void OnPopulateMesh(VertexHelper mesh){
  mesh.Clear();float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
  Vector2 center=rectTransform.rect.center;const int segments=64;
  mesh.AddVert(center,color,Vector2.zero);
  for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius-2),color,Vector2.zero);}
  for(int i=0;i<segments;i++)mesh.AddTriangle(0,i+1,i+2);
  Color rim=rimColor;
  int start=mesh.currentVertCount;
  for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));mesh.AddVert(center+d*(radius-2),rim,Vector2.zero);mesh.AddVert(center+d*radius,rim,Vector2.zero);}
  for(int i=0;i<segments;i++){int v=start+i*2;mesh.AddTriangle(v,v+1,v+2);mesh.AddTriangle(v+1,v+3,v+2);}
  if(arrow){
   int v=mesh.currentVertCount;float s=radius*.52f;
   Vector2[] points={new Vector2(-.38f,-.65f),new Vector2(.38f,-.65f),new Vector2(.38f,.05f),new Vector2(-.38f,.05f),new Vector2(-.85f,.05f),new Vector2(.85f,.05f),new Vector2(0,.95f)};
   foreach(var p in points)mesh.AddVert(center+p*s,rim,Vector2.zero);
   mesh.AddTriangle(v,v+1,v+2);mesh.AddTriangle(v,v+2,v+3);mesh.AddTriangle(v+4,v+5,v+6);
  }
 }
}
}
