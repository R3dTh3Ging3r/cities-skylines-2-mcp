using System;
using System.Linq;
using CS2MCP.Core;

namespace CS2MCP.CoreTests
{
    internal static class NetworkGeometryDiagnosticTests
    {
        static Point3 P(double x,double y,double z=0)=>new Point3(x,y,z);
        static CubicCurve Curve(Point3 a,Point3 b,Point3 c,Point3 d)=>new CubicCurve{a=a,b=b,c=c,d=d};
        static CurveDiagnosticProfile Profile(CubicCurve curve)=>NetworkGeometryDiagnostics.Analyze(new EntityRef{world=new WorldRef("boot",1),index=1,version=1},curve);
        static void Near(double expected,double? actual,double tolerance=1e-10)
        {
            if(!actual.HasValue||Math.Abs(expected-actual.Value)>tolerance*Math.Max(1,Math.Abs(expected)))throw new Exception($"expected {expected}, got {actual}");
        }
        internal static void Run()
        {
            Program.Check("constant signed grade and reversed cubic preserve17 exact parameter positions",()=>{
                var c=Curve(P(0,0),P(1,2),P(2,4),P(3,6));var p=Profile(c);
                Program.Equal(17,p.samples.Length);
                for(int i=0;i<17;i++) {var s=p.samples[i];Near(i/16d,s.t);Near(3*s.t,s.position.x);Near(6*s.t,s.position.y);Near(2,s.grade.value);Near(0,s.curvature.value);Program.Equal(null,s.radius.value);Program.Equal("zeroCurvatureUnboundedRadius",s.radius.reason);}
                Near(0,p.grade.min.t);Near(0,p.grade.max.t);Program.Equal(17,p.grade.supportedSamples);Program.Equal(17,p.radius.unavailableSamples);
                var reverse=Profile(Curve(c.d,c.c,c.b,c.a));Near(-2,reverse.samples[8].grade.value);
            });
            Program.Check("analytic spatial quadratic embedded in cubic has independent grade curvature and radius",()=>{
                // P(t)=(t,t^2,t): P'(0)=(1,0,1), P''=(0,2,0), k(0)=1.
                var p=Profile(Curve(P(0,0,0),P(1d/3,0,1d/3),P(2d/3,1d/3,2d/3),P(1,1,1)));
                Near(0,p.samples[0].grade.value);Near(1,p.samples[0].curvature.value);Near(1,p.samples[0].radius.value);
                Near(1/Math.Sqrt(2),p.samples[8].grade.value);Near(1/Math.Pow(1.5,1.5),p.samples[8].curvature.value);
                var reversed=Profile(Curve(P(1,1,1),P(2d/3,1d/3,2d/3),P(1d/3,0,1d/3),P(0,0,0)));
                Near(p.samples[4].curvature.value.Value,reversed.samples[12].curvature.value);
                Near(-p.samples[4].grade.value.Value,reversed.samples[12].grade.value);
                Near(1,p.curvature.max.value);Near(0,p.curvature.max.t);Near(0.5,p.samples[8].position.x);Near(.25,p.samples[8].position.y);
            });
            Program.Check("vertical stationary and coincident derivatives keep all rows and quantity-specific reasons",()=>{
                var vertical=Profile(Curve(P(0,0),P(0,1),P(0,2),P(0,3)));
                Program.Equal(null,vertical.samples[8].grade.value);Program.Equal("zeroHorizontalDerivative",vertical.samples[8].grade.reason);Near(0,vertical.samples[8].curvature.value);
                var stationary=Profile(Curve(P(0,0),P(0,0),P(1,1),P(2,1)));
                Program.Equal("stationaryDerivative",stationary.samples[0].curvature.reason);Program.Equal(1,stationary.curvature.unavailableSamples);
                var zero=Profile(Curve(P(1,1),P(1,1),P(1,1),P(1,1)));
                Program.Equal(17,zero.grade.unavailableSamples);Program.Equal(null,zero.grade.min);Program.Equal(17,zero.samples.Length);Near(1,zero.samples[8].position.x);
            });
            Program.Check("scaled arithmetic supports tiny and large finite geometry without flattening tiny nonzero grade",()=>{
                foreach(double scale in new[]{1e-150,1e150,1e307}) {
                    var p=Profile(Curve(P(0,0),P(scale,2*scale),P(2*scale,4*scale),P(3*scale,6*scale)));
                    Near(2,p.samples[8].grade.value);Near(0,p.samples[8].curvature.value);
                }
                var tiny=Profile(Curve(P(0,0),P(1,1e-150),P(2,2e-150),P(3,3e-150)));
                Program.Equal(true,tiny.samples[8].grade.value>0);Near(1e-150,tiny.samples[8].grade.value);
            });
            Program.Check("overflow and conditioning return explicit per-quantity unavailable evidence",()=>{
                var small=Profile(Curve(P(0,0),P(1e-310,0),P(2e-310,1e-310),P(3e-310,1e-310)));
                Program.Equal(null,small.samples[0].curvature.value);Program.Equal("arithmeticOverflow",small.samples[0].curvature.reason);
                Program.Equal(true,small.samples[0].radius.value>0);Near(0,small.samples[0].grade.value);
                var huge=Profile(Curve(P(0,0),P(2.5e307,0),P(5e307,1e-12),P(7.5e307,1e-12)));
                Program.Equal("arithmeticUnderflow",huge.samples[0].curvature.reason);Program.Equal("arithmeticOverflow",huge.samples[0].radius.reason);
                var mixed=Profile(Curve(P(0,0),P(1e-300,1e300),P(2e-300,2e300),P(3e-300,3e300)));
                Program.Equal("controlNormalizationLostPrecision",mixed.samples[8].grade.reason);Program.Equal(true,mixed.samples[8].position!=null);
            });
            Program.Check("sampled extrema do not claim an unsampled global maximum",()=>{
                // x=(t-.03)^2 + .001t, y=t. The horizontal tangent vanishes
                // at .0295 between our grid values, so sampled grades cannot bound it.
                var p=Profile(Curve(P(.0009,0),P(.0009-.059/3,1d/3),P(.0009-.118/3+1d/3,2d/3),P(.9419,1)));
                Program.Equal("returnedSamplesOnly",p.extremaCoverage);Program.Equal(17,p.samples.Length);
                Program.Equal(true,p.grade.max!=null);Program.Equal(true,p.grade.min!=null);Program.Equal(true,1000>p.grade.max.value);
            });
            Program.Check("nonzero derivative interpolation lost below representability never becomes supported flat geometry",()=>{
                var p=Profile(Curve(P(0,0),P(0,double.Epsilon),P(1,double.Epsilon),P(1,double.Epsilon)));
                Program.Equal(null,p.samples[8].grade.value);
                Program.Equal(null,p.samples[8].curvature.value);
                Program.Equal(null,p.samples[8].radius.value);
            });
            Program.Check("constant tiny coordinate translation does not invalidate unaffected derivatives",()=>{
                var p=Profile(Curve(P(0,1e-300),P(1e300,1e-300),P(2e300,1e-300),P(3e300,1e-300)));
                Near(0,p.samples[8].grade.value);Near(0,p.samples[8].curvature.value);
                Program.Equal(true,p.samples[8].position.y>0);
            });
            Program.Check("actual producer preserves translated straight and bowed derivative evidence and summaries",()=>{
                var world=new WorldIdentity("boot");world.BeginLoad();world.CompleteLoad();
                foreach(double offset in new[]{0d,100000d,1e16}) foreach(bool reversed in new[]{false,true}) {
                    var straight=Curve(P(offset,0),P(offset+2,2),P(offset+4,4),P(offset+6,6));
                    var bowed=Curve(P(offset,0,0),P(offset+2,0,2),P(offset+4,2,4),P(offset+6,6,6));
                    if(reversed) {straight=Curve(straight.d,straight.c,straight.b,straight.a);bowed=Curve(bowed.d,bowed.c,bowed.b,bowed.a);}
                    var query=ConstructionInventory.DecodeQuery("bootId=boot&epoch=1&entities=1:1,2:1&geometryDiagnostics=sampledCubicV1");
                    var observed=ConstructionInventory.Read(query,world,()=>10,e=>new ConstructionEntity{entity=e,kind="networkEdge",networkReason=null,
                        network=new NetworkGeometry{curve=e.index==1?straight:bowed,start=e,end=e,length=99}});
                    var line=observed.data.geometryDiagnostics.profiles[0];
                    var bend=observed.data.geometryDiagnostics.profiles[1];
                    for(int i=0;i<17;i++) {
                        Near(reversed?-1:1,line.samples[i].grade.value);Near(0,line.samples[i].curvature.value,0);
                        Program.Equal("zeroCurvatureUnboundedRadius",line.samples[i].radius.reason);
                        double t=reversed?1-i/16d:i/16d;
                        Near((reversed?-1:1)*Math.Sqrt(2)*t,bend.samples[i].grade.value);
                        Near(1/(6*Math.Pow(1+2*t*t,1.5)),bend.samples[i].curvature.value);
                        Near(6*Math.Pow(1+2*t*t,1.5),bend.samples[i].radius.value);
                    }
                    Near(reversed?-1:1,line.grade.min.value);Near(0,line.curvature.max.value,0);Near(0,line.curvature.max.t);
                    Near(1d/6,bend.curvature.max.value);Near(reversed?1:0,bend.curvature.max.t);
                    Near(reversed?straight.a.x:offset,observed.data.entities[0].network.curve.a.x,0);
                    Program.Equal(34,observed.data.geometryDiagnostics.evaluations);Near(99,observed.data.entities[0].network.length);
                }
            });
            Program.Check("overflowing local subtraction preserves straight derivatives through scaled opposite endpoints",()=>{
                var p=Profile(Curve(P(-1e308,-1e308),P(1e308,1e308),P(1e308,1e308),P(1e308,1e308)));
                for(int i=0;i<16;i++) {
                    Near(1,p.samples[i].grade.value);Near(0,p.samples[i].curvature.value,0);
                    Program.Equal("zeroCurvatureUnboundedRadius",p.samples[i].radius.reason);
                }
                Program.Equal("stationaryDerivative",p.samples[16].curvature.reason);
                Program.Equal(true,p.samples[8].position!=null);
            });
        }
    }
}
