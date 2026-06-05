using KERBALISM;

namespace KerbalismProcess
{
	public sealed class SystemHeatProcessDevice : LoadedDevice<ProcessControllerSystemHeat>
	{
		public SystemHeatProcessDevice(ProcessControllerSystemHeat module) : base(module) { }

		public override bool IsVisible => module.toggle;

		public override string DisplayName => module.title;

		public override string Tooltip => Lib.BuildString(base.Tooltip, "\n", Lib.Bold("Process capacity :"), "\n", module.ModuleInfo);

		public override string Status => Lib.Color(module.IsRunning(), Local.Generic_RUNNING, Lib.Kolor.Green, Local.Generic_STOPPED, Lib.Kolor.Yellow);

		public override void Ctrl(bool value) => module.SetRunning(value);

		public override void Toggle() => Ctrl(!module.IsRunning());
	}

	public sealed class ProtoSystemHeatProcessDevice : ProtoDevice<ProcessControllerSystemHeat>
	{
		public ProtoSystemHeatProcessDevice(ProcessControllerSystemHeat prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule) { }

		public override bool IsVisible => prefab.toggle;

		public override string DisplayName => prefab.title;

		public override string Tooltip => Lib.BuildString(base.Tooltip, "\n", Lib.Bold("Process capacity :"), "\n", prefab.ModuleInfo);

		public override string Status => Lib.Color(Lib.Proto.GetBool(protoModule, nameof(ProcessController.running)), Local.Generic_RUNNING, Lib.Kolor.Green, Local.Generic_STOPPED, Lib.Kolor.Yellow);

		public override void Ctrl(bool value)
		{
			if (Lib.Proto.GetBool(protoModule, nameof(ProcessController.broken)))
				return;

			Lib.Proto.Set(protoModule, nameof(ProcessController.running), value);
			var res = protoPart.resources.Find(k => k.resourceName == prefab.resource);
			if (res != null) res.flowState = value;
		}

		public override void Toggle() => Ctrl(!Lib.Proto.GetBool(protoModule, nameof(ProcessController.running)));
	}

	public sealed class SystemHeatHarvesterDevice : LoadedDevice<HarvesterSystemHeat>
	{
		private readonly ModuleAnimationGroup animator;

		public SystemHeatHarvesterDevice(HarvesterSystemHeat module) : base(module)
		{
			animator = module.part.FindModuleImplementing<ModuleAnimationGroup>();
		}

		public override string Name => Lib.BuildString(module.resource, " harvester").ToLower();

		public override string Status => animator != null && !module.deployed
			? Local.Generic_notdeployed
			: !module.running
				? Lib.Color(Local.Generic_STOPPED, Lib.Kolor.Yellow)
				: module.issue.Length == 0
					? Lib.Color(Local.Generic_RUNNING, Lib.Kolor.Green)
					: Lib.Color(module.issue, Lib.Kolor.Red);

		public override void Ctrl(bool value)
		{
			if (module.deployed)
				module.running = value;
		}

		public override void Toggle() => Ctrl(!module.running);
	}

	public sealed class ProtoSystemHeatHarvesterDevice : ProtoDevice<HarvesterSystemHeat>
	{
		private readonly ProtoPartModuleSnapshot animator;

		public ProtoSystemHeatHarvesterDevice(HarvesterSystemHeat prefab, ProtoPartSnapshot protoPart, ProtoPartModuleSnapshot protoModule)
			: base(prefab, protoPart, protoModule)
		{
			animator = protoPart.FindModule("ModuleAnimationGroup");
		}

		public override string Name => Lib.BuildString(prefab.resource, " harvester").ToLower();

		public override string Status
		{
			get
			{
				bool deployed = Lib.Proto.GetBool(protoModule, "deployed");
				bool running = Lib.Proto.GetBool(protoModule, "running");
				string issue = Lib.Proto.GetString(protoModule, "issue");

				return animator != null && !deployed
					? Local.Generic_notdeployed
					: !running
						? Lib.Color(Local.Generic_STOPPED, Lib.Kolor.Yellow)
						: issue.Length == 0
							? Lib.Color(Local.Generic_RUNNING, Lib.Kolor.Green)
							: Lib.Color(issue, Lib.Kolor.Red);
			}
		}

		public override void Ctrl(bool value)
		{
			if (Lib.Proto.GetBool(protoModule, "deployed"))
				Lib.Proto.Set(protoModule, "running", value);
		}

		public override void Toggle() => Ctrl(!Lib.Proto.GetBool(protoModule, "running"));
	}
}
