using NUnit.Framework;
using RobotFacility.Telemetry;
using UnityEngine;

namespace RobotFacility.Tests
{
    public class TelemetryValidatorTests
    {
        private static readonly Rect Bounds = new Rect(-50f, -50f, 100f, 100f);
        private const float MaxSpeed = 15f;

        private static RawTelemetry Good() => new RawTelemetry
        {
            unitId = "BOT_001", name = "ROBOT_A", timestamp = "2026-10-07T10:30:05",
            x = 24.5f, z = 18.7f, heading = 135f, speed = 4.2f
        };

        private static bool Validate(RawTelemetry r, out TelemetryUpdate u, out string err) =>
            TelemetryValidator.TryValidate(r, Bounds, MaxSpeed, out u, out err);

        [Test]
        public void ValidRecord_IsAccepted()
        {
            Assert.IsTrue(Validate(Good(), out var u, out _));
            Assert.AreEqual("BOT_001", u.UnitId);
            Assert.AreEqual(135f, u.Heading, 0.001f);
        }

        [Test] public void Null_IsRejected() => Assert.IsFalse(Validate(null, out _, out _));

        [TestCase(null)] [TestCase("")] [TestCase("   ")]
        public void EmptyUnitId_IsRejected(string id)
        {
            var r = Good(); r.unitId = id;
            Assert.IsFalse(Validate(r, out _, out var err));
            StringAssert.Contains("unitId", err);
        }

        [Test] public void MissingX_IsRejected() { var r = Good(); r.x = null; Assert.IsFalse(Validate(r, out _, out _)); }
        [Test] public void MissingZ_IsRejected() { var r = Good(); r.z = null; Assert.IsFalse(Validate(r, out _, out _)); }
        [Test] public void NaNPosition_IsRejected() { var r = Good(); r.x = float.NaN; Assert.IsFalse(Validate(r, out _, out _)); }
        [Test] public void OutOfBounds_IsRejected() { var r = Good(); r.x = 500f; Assert.IsFalse(Validate(r, out _, out _)); }
        [Test] public void NegativeSpeed_IsRejected() { var r = Good(); r.speed = -1f; Assert.IsFalse(Validate(r, out _, out _)); }
        [Test] public void ImplausibleSpeed_IsRejected() { var r = Good(); r.speed = 999f; Assert.IsFalse(Validate(r, out _, out _)); }
        [Test] public void MissingSpeed_IsRejected() { var r = Good(); r.speed = null; Assert.IsFalse(Validate(r, out _, out _)); }

        [TestCase(-1f)] [TestCase(361f)] [TestCase(999f)]
        public void InvalidHeading_IsRejected(float h) { var r = Good(); r.heading = h; Assert.IsFalse(Validate(r, out _, out _)); }

        [Test]
        public void Heading360_IsNormalisedTo0()
        {
            var r = Good(); r.heading = 360f;
            Assert.IsTrue(Validate(r, out var u, out _));
            Assert.AreEqual(0f, u.Heading, 0.001f);
        }

        [Test]
        public void MissingName_FallsBackToUnitId()
        {
            var r = Good(); r.name = null;
            Assert.IsTrue(Validate(r, out var u, out _));
            Assert.AreEqual("BOT_001", u.Name);
        }

        [Test]
        public void Parser_RejectsMalformedJson_WithoutThrowing()
        {
            Assert.IsFalse(TelemetryParser.TryParse("{ not json", out _, out var err));
            Assert.IsNotEmpty(err);
        }

        [Test]
        public void Parser_DistinguishesMissingFromZero()
        {
            Assert.IsTrue(TelemetryParser.TryParse("{\"unitId\":\"A\",\"z\":0,\"heading\":0,\"speed\":0}", out var raw, out _));
            Assert.IsFalse(raw.x.HasValue);
            Assert.AreEqual(0f, raw.z.Value);
        }
    }
}
