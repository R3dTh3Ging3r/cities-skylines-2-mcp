using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2MCP.Core
{
    public sealed class DiagnosticQuantity
    {
        public double? value { get; set; }
        public string support => value.HasValue ? "supported" : "unavailable";
        public string reason { get; set; }
    }
    public sealed class CurveDiagnosticSample
    {
        public double t { get; set; }
        public Point3 position { get; set; }
        public string positionReason { get; set; }
        public DiagnosticQuantity grade { get; set; }
        public DiagnosticQuantity curvature { get; set; }
        public DiagnosticQuantity radius { get; set; }
    }
    public sealed class CurveDiagnosticExtremum
    {
        public double value { get; set; }
        public double t { get; set; }
        public Point3 position { get; set; }
        public string positionReason { get; set; }
    }
    public sealed class CurveDiagnosticSummary
    {
        public int supportedSamples { get; set; }
        public int unavailableSamples { get; set; }
        public CurveDiagnosticExtremum min { get; set; }
        public CurveDiagnosticExtremum max { get; set; }
    }
    public sealed class CurveDiagnosticProfile
    {
        public EntityRef entity { get; set; }
        public string status { get; set; }
        public string reason { get; set; }
        public string extremaCoverage => "returnedSamplesOnly";
        public CurveDiagnosticSample[] samples { get; set; } = Array.Empty<CurveDiagnosticSample>();
        public CurveDiagnosticSummary grade { get; set; }
        public CurveDiagnosticSummary curvature { get; set; }
        public CurveDiagnosticSummary radius { get; set; }
    }
    public sealed class GeometryDiagnosticSnapshot
    {
        public string mode => "sampledCubicV1";
        public string coordinates => "nativeWorldXYZ";
        public string verticalAxis => "Y";
        public string direction => "aToDNotLaneDirection";
        public string gradeUnit => "dimensionlessRiseRun";
        public string curvatureUnit => "inverseNativeWorldCoordinate";
        public string radiusUnit => "nativeWorldCoordinate";
        public string physicalUnits => "unverified";
        public string engineeringValidation => "unknown";
        public string sampling => "uniformParameter";
        public string betweenSamples => "unmeasured";
        public int samplesPerCurve => NetworkGeometryDiagnostics.SamplesPerCurve;
        public int maxRoots => ConstructionInventory.MaxEntities;
        public int maxEvaluations => NetworkGeometryDiagnostics.MaxEvaluations;
        public int evaluations { get; set; }
        public CurveDiagnosticProfile[] profiles { get; set; }
    }

    /// <summary>Consumed bounded double-precision diagnostics of captured native cubics.
    /// Samples are not continuous extrema, engineering limits, native preview or lane proof.</summary>
    public static class NetworkGeometryDiagnostics
    {
        public const int SamplesPerCurve = 17;
        public const int MaxEvaluations = ConstructionInventory.MaxEntities * SamplesPerCurve;
        static bool Finite(double x) => ConstructionGeometry.Finite(x);
        static DiagnosticQuantity Missing(string reason) => new DiagnosticQuantity { reason = reason };
        static DiagnosticQuantity Known(double value) => new DiagnosticQuantity { value = value };
        static double Max(Point3 p) => Math.Max(Math.Abs(p.x), Math.Max(Math.Abs(p.y), Math.Abs(p.z)));
        static Point3 Sub(Point3 a, Point3 b) => new Point3(a.x-b.x,a.y-b.y,a.z-b.z);
        static Point3 Multiply(Point3 p,double k) => new Point3(p.x*k,p.y*k,p.z*k);
        static Point3 Lerp(Point3 a,Point3 b,double t,ref bool lost)
        {
            bool localLost=false;
            double Blend(double x,double y)
            {
                double left=x*(1-t),right=y*t;
                localLost|=Underflow(x,1-t,left)||Underflow(y,t,right);
                return left+right;
            }
            var result=new Point3(Blend(a.x,b.x),Blend(a.y,b.y),Blend(a.z,b.z));
            lost|=localLost;
            return result;
        }
        static double Norm(Point3 p)
        {
            double scale=Max(p);
            if(scale==0)return 0;
            double x=p.x/scale,y=p.y/scale,z=p.z/scale;
            return scale*Math.Sqrt(x*x+y*y+z*z);
        }
        static bool Underflow(double a,double b,double product) => a!=0 && b!=0 && product==0;
        static Point3 Position(CubicCurve curve,double t,out string reason)
        {
            reason=null;
            if(t==0)return new Point3(curve.a.x,curve.a.y,curve.a.z);
            if(t==1)return new Point3(curve.d.x,curve.d.y,curve.d.z);
            bool lost=false;
            Point3 Blend(Point3 a,Point3 b)
            {
                double BlendScalar(double x,double y)
                {
                    double left=x*(1-t),right=y*t;
                    lost |= Underflow(x,1-t,left)||Underflow(y,t,right);
                    return left+right;
                }
                return new Point3(BlendScalar(a.x,b.x),BlendScalar(a.y,b.y),BlendScalar(a.z,b.z));
            }
            var value=Blend(Blend(Blend(curve.a,curve.b),Blend(curve.b,curve.c)),Blend(Blend(curve.b,curve.c),Blend(curve.c,curve.d)));
            if(lost||!Finite(value.x)||!Finite(value.y)||!Finite(value.z)) { reason="positionArithmeticUnavailable";return null; }
            return value;
        }
        static DiagnosticQuantity Exp(double logarithm)
        {
            double value=Math.Exp(logarithm);
            return !Finite(value)?Missing("arithmeticOverflow"):value==0?Missing("arithmeticUnderflow"):Known(value);
        }
        static CurveDiagnosticSummary Summary(CurveDiagnosticSample[] samples,Func<CurveDiagnosticSample,DiagnosticQuantity> select)
        {
            var result=new CurveDiagnosticSummary();
            foreach(var sample in samples)
            {
                var value=select(sample).value;
                if(!value.HasValue){result.unavailableSamples++;continue;}
                result.supportedSamples++;
                var item=new CurveDiagnosticExtremum { value=value.Value,t=sample.t,position=sample.position,positionReason=sample.positionReason };
                if(result.min==null||value<result.min.value)result.min=item;
                if(result.max==null||value>result.max.value)result.max=item;
            }
            return result;
        }
        internal static CurveDiagnosticProfile Analyze(EntityRef entity,CubicCurve curve)
        {
            ConstructionGeometry.Validate(curve);
            var controls=new[]{curve.a,curve.b,curve.c,curve.d};
            // Subtract captured coordinates first. Normalizing absolute positions
            // before subtraction can distort small edges at a large world origin.
            var edges=new[]{Sub(curve.b,curve.a),Sub(curve.c,curve.b),Sub(curve.d,curve.c)};
            bool overflow=edges.Any(e=>!Finite(e.x)||!Finite(e.y)||!Finite(e.z));
            double scale=overflow?controls.Max(Max):edges.Max(Max);
            bool lost=false;
            Point3 NormalizeEdge(int index)
            {
                double Component(double difference,double start,double end)
                {
                    // Only an overflowing subtraction uses scaled endpoints. Such
                    // endpoints have opposite signs, so this does not cancel a
                    // small local difference. Finite differences stay local even
                    // when another edge requires the larger fallback scale.
                    double value=Finite(difference)?(scale==0?0:difference/scale):end/scale-start/scale;
                    if(difference!=0&&value==0)lost=true;
                    return value;
                }
                var edge=edges[index];var a=controls[index];var b=controls[index+1];
                return new Point3(Component(edge.x,a.x,b.x),Component(edge.y,a.y,b.y),Component(edge.z,a.z,b.z));
            }
            var e0=NormalizeEdge(0);var e1=NormalizeEdge(1);var e2=NormalizeEdge(2);
            var rows=new CurveDiagnosticSample[SamplesPerCurve];
            for(int i=0;i<rows.Length;i++)
            {
                double t=i/16d;
                var position=Position(curve,t,out string positionReason);
                var row=new CurveDiagnosticSample {t=t,position=position,positionReason=positionReason};
                rows[i]=row;
                if(lost) {row.grade=Missing("controlNormalizationLostPrecision");row.curvature=Missing("controlNormalizationLostPrecision");row.radius=Missing("controlNormalizationLostPrecision");continue;}
                bool firstLost=false,secondLost=false;
                var first=Multiply(Lerp(Lerp(e0,e1,t,ref firstLost),Lerp(e1,e2,t,ref firstLost),t,ref firstLost),3);
                var second=Multiply(Lerp(Sub(e1,e0),Sub(e2,e1),t,ref secondLost),6);
                double horizontal=Norm(new Point3(first.x,0,first.z));
                double grade=first.y/horizontal;
                row.grade=firstLost?Missing("arithmeticUnderflow"):horizontal==0?Missing("zeroHorizontalDerivative"):!Finite(grade)?Missing("arithmeticOverflow"):
                    first.y!=0&&grade==0?Missing("arithmeticUnderflow"):Known(grade);
                double speed=Norm(first),acceleration=Max(second);
                if(firstLost||secondLost) {row.curvature=Missing("derivativeArithmeticUnavailable");row.radius=Missing("derivativeArithmeticUnavailable");continue;}
                if(speed==0) {row.curvature=Missing("stationaryDerivative");row.radius=Missing("stationaryDerivative");continue;}
                var u=new Point3(first.x/speed,first.y/speed,first.z/speed);
                var v=acceleration==0?new Point3(0,0,0):new Point3(second.x/acceleration,second.y/acceleration,second.z/acceleration);
                bool NormalizationLost(Point3 original,Point3 normalized) =>
                    original.x!=0&&normalized.x==0||original.y!=0&&normalized.y==0||original.z!=0&&normalized.z==0;
                if(NormalizationLost(first,u)||NormalizationLost(second,v)) {row.curvature=Missing("derivativeArithmeticUnavailable");row.radius=Missing("derivativeArithmeticUnavailable");continue;}
                // Scaled unit-vector products avoid squared/cubed world coordinates.
                bool crossLost=false;
                double Product(double a,double b){double value=a*b;crossLost|=Underflow(a,b,value);return value;}
                var cross=new Point3(Product(u.y,v.z)-Product(u.z,v.y),Product(u.z,v.x)-Product(u.x,v.z),Product(u.x,v.y)-Product(u.y,v.x));
                double bend=Norm(cross);
                if(!Finite(bend)||crossLost) {row.curvature=Missing("derivativeArithmeticUnavailable");row.radius=Missing("derivativeArithmeticUnavailable");continue;}
                if(bend==0) {row.curvature=Known(0);row.radius=Missing("zeroCurvatureUnboundedRadius");continue;}
                // Log magnitudes avoid overflow/underflow in speed^2*worldScale.
                // Compute each reciprocal independently: one may be finite when the other is not.
                double logCurvature=Math.Log(acceleration)+Math.Log(bend)-2*Math.Log(speed)-Math.Log(scale);
                row.curvature=Exp(logCurvature);row.radius=Exp(-logCurvature);
            }
            return new CurveDiagnosticProfile {entity=entity,status="available",samples=rows,
                grade=Summary(rows,s=>s.grade),curvature=Summary(rows,s=>s.curvature),radius=Summary(rows,s=>s.radius)};
        }
        internal static GeometryDiagnosticSnapshot Read(ConstructionEntity[] roots,WorldIdentity worlds,WorldRef world)
        {
            worlds.Validate(world);
            var profiles=new CurveDiagnosticProfile[roots.Length];int evaluations=0;
            for(int i=0;i<roots.Length;i++)
            {
                worlds.Validate(world);
                var root=roots[i];
                profiles[i]=root.network==null?new CurveDiagnosticProfile {entity=root.entity,status="unavailable",reason=root.unavailableReason??root.networkReason??"networkCurveUnavailable"}:
                    Analyze(root.entity,root.network.curve);
                if(root.network!=null)evaluations+=SamplesPerCurve;
                worlds.Validate(world);
            }
            return new GeometryDiagnosticSnapshot {profiles=profiles,evaluations=evaluations};
        }
    }
}
