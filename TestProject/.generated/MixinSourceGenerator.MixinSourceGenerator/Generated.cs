#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
namespace MixinSourceGeneratorTestProject {
	partial class TargetClass {
		private MixinSourceGeneratorTestProject.MixinClass MixinSourceGeneratorTestProject_MixinClass = new MixinSourceGeneratorTestProject.MixinClass();
		new public System.String MixinProperty {
			get {
				var previousAggregator = Aggregator.Current;
				try {
					Aggregator.Current = this;
					return MixinSourceGeneratorTestProject_MixinClass.MixinProperty;
				}
				finally {
					Aggregator.Current = previousAggregator;
				}
			}
			set {
				var previousAggregator = Aggregator.Current;
				try {
					Aggregator.Current = this;
					MixinSourceGeneratorTestProject_MixinClass.MixinProperty = value;
				}
				finally {
					Aggregator.Current = previousAggregator;
				}
			}
		}
	}
}
