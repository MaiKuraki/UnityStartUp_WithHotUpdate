using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"MessagePipe.Zenject.dll",
		"UniTask.dll",
		"UnityEngine.CoreModule.dll",
		"Zenject.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<StartUp.Gameplay.DemoScript.<UnloadLaunchScene>d__4>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<StartUp.Gameplay.DemoScript.<UnloadLaunchScene>d__4>
	// Cysharp.Threading.Tasks.ITaskPoolNode<object>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<Cysharp.Threading.Tasks.AsyncUnit>
	// System.Action<object>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.Queue.Enumerator<object>
	// System.Collections.Generic.Queue<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<object>
	// System.Func<int>
	// System.Func<object,object,object>
	// System.Predicate<object>
	// Zenject.DiContainer.<>c__DisplayClass206_0<object>
	// }}

	public void RefMethods()
	{
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UnityAsyncExtensions.AsyncOperationAwaiter,StartUp.Gameplay.DemoScript.<UnloadLaunchScene>d__4>(Cysharp.Threading.Tasks.UnityAsyncExtensions.AsyncOperationAwaiter&,StartUp.Gameplay.DemoScript.<UnloadLaunchScene>d__4&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<StartUp.Gameplay.DemoScript.<UnloadLaunchScene>d__4>(StartUp.Gameplay.DemoScript.<UnloadLaunchScene>d__4&)
		// Zenject.DiContainer MessagePipe.DiContainerExtensions.BindMessageBroker<object>(Zenject.DiContainer,MessagePipe.MessagePipeOptions)
		// object UnityEngine.Object.FindObjectOfType<object>()
		// Zenject.IdScopeConcreteIdArgConditionCopyNonLazyBinder Zenject.DiContainer.BindInstance<object>(object)
		// Zenject.FromBinderNonGeneric Zenject.DiContainer.BindInterfacesTo<object>()
	}
}