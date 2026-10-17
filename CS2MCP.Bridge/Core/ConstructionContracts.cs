using System;

namespace CS2MCP.Core
{
    public sealed class Point3
    {
        public double x { get; set; } public double y { get; set; } public double z { get; set; }
        public Point3(double x,double y,double z){this.x=x;this.y=y;this.z=z;}
    }
    public sealed class Rotation4
    {
        public double x { get; } public double y { get; } public double z { get; } public double w { get; }
        public Rotation4(double x,double y,double z,double w){this.x=x;this.y=y;this.z=z;this.w=w;}
    }
    public sealed class CubicCurve { public Point3 a { get; set; } public Point3 b { get; set; } public Point3 c { get; set; } public Point3 d { get; set; } }
    public sealed class OrientedBounds
    {
        public Point3 center { get; set; } public Point3 halfExtents { get; set; }
        public Point3 axisX { get; set; } public Point3 axisY { get; set; } public Point3 axisZ { get; set; }
        public string scope => "rootGeometryOnly";
    }
    public sealed class ConstructionQuery { public ConstructionPageQuery relationshipPage { get; internal set; } public WorldRef world { get; internal set; } public EntityRef[] entities { get; internal set; } public string relationships { get; internal set; } public string geometryDiagnostics { get; internal set; } }
    public sealed class NetworkGeometry
    {
        public CubicCurve curve { get; set; } public EntityRef start { get; set; } public EntityRef end { get; set; }
        public double length { get; set; }
        public string direction => "startToEndNotLaneDirection";
        public string coordinates => "worldXYZ";
        public double? width { get; set; } public string widthReason { get; set; } = "prefabGeometryUnavailable";
        public string requiredLayers { get; set; } public string connectLayers { get; set; }
        public string layersReason { get; set; } = "prefabNetDataUnavailable";
        public object elevationMode => null; public string elevationModeReason => "notVerified";
        public object laneDirections => null; public string laneDirectionsReason => "notMeasured";
    }
    public sealed class ConstructionEntity
    {
        public EntityRef entity { get; set; } public string kind { get; set; }
        public string unavailableReason { get; set; }
        public string assetKey { get; set; } public string assetKeyReason { get; set; } = "prefabUnavailable";
        public EntityRef owner { get; set; } public string ownerReason { get; set; } = "noOwnerComponent";
        public NetworkGeometry network { get; set; } public string networkReason { get; set; } = "notNetworkEdge";
        public OrientedBounds rootBounds { get; set; } public string rootBoundsReason { get; set; } = "notBuilding";
        public string family => null; public string familyReason => "notVerified";
        public object compatibleFamilies => null; public string compatibleFamiliesReason => "notVerified";
        public object installedUpgrades => null; public string installedUpgradesReason { get; set; } = "notMeasured";
        public object facilityEnvelope => null; public string facilityEnvelopeReason => "notMeasured";
        public object childEnvelope => null; public string childEnvelopeReason => "notMeasured";
        public object platformEnvelope => null; public string platformEnvelopeReason => "notMeasured";
        public object approachEnvelope => null; public string approachEnvelopeReason => "notMeasured";
        public object connectors => null; public string connectorsReason => "notMeasured";
        public object costs => null; public string costsReason => "notMeasured";
        public object prerequisites => null; public string prerequisitesReason => "notVerified";
        public object capacityEffects => null; public string capacityEffectsReason => "notMeasured";
    }
    public sealed class ConstructionSnapshot
    {
        public string scope => "explicitEntities";
        public EntityRef[] requestedEntities { get; set; } public ConstructionEntity[] entities { get; set; }
        public ConstructionRelationshipSnapshot relationships { get; set; }
        public ConstructionPageSnapshot relationshipPage { get; set; }
        public GeometryDiagnosticSnapshot geometryDiagnostics { get; set; }
        public object worldBounds => null; public string worldBoundsReason => "notMeasured";
        public string nextCursor => null; public bool truncated => false;
        public string support => "partial"; public string nativeVerification => "CON-01:notRun";
    }
}
