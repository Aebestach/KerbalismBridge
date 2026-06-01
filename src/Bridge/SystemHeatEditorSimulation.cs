using System;
using UnityEngine;

namespace KerbalismBridge
{
	public static class SystemHeatEditorSimulation
	{
		public const double MinEff = 1e-5;
		public const double MaxEff = 1.5;
		public const double HystFrac = 1e-3;

		public static bool IsEditorScene => HighLogic.LoadedSceneIsEditor;

		public static double EvaluateEfficiency(FloatCurve curve, float loopTemperatureK)
		{
			if (curve == null)
				return 1.0;

			double eff = curve.Evaluate(loopTemperatureK);
			return Math.Max(MinEff, Math.Min(MaxEff, eff));
		}
	}
}
