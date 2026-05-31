using System;
using UnityEngine;

namespace KerbalismSystemHeat
{
	internal static class SystemHeatEditorSimulation
	{
		internal const double MinEff = 1e-5;
		internal const double MaxEff = 1.5;
		internal const double HystFrac = 1e-3;

		internal static bool IsEditorScene => HighLogic.LoadedSceneIsEditor;

		internal static double EvaluateEfficiency(FloatCurve curve, float loopTemperatureK)
		{
			if (curve == null)
				return 1.0;

			double eff = curve.Evaluate(loopTemperatureK);
			return Math.Max(MinEff, Math.Min(MaxEff, eff));
		}
	}
}
