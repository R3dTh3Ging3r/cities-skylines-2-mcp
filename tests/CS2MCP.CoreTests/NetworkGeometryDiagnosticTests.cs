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
        }
    }
}
