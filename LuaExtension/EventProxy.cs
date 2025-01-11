using System;
using System.Reflection;

using MoonSharp.Interpreter;


namespace LuaExtension
{
	[MoonSharpUserData]
	public class EventProxy<TEventArgs>
	{
		private readonly EventInfo _underlying;
		private readonly object    _target;

		public EventProxy(EventInfo underlying, object target)
		{
			_underlying = underlying;
			_target     = target;
		}

		public void Add(EventHandler<TEventArgs>    handler) => _underlying.AddEventHandler(_target, handler);
		public void Remove(EventHandler<TEventArgs> handler) => _underlying.RemoveEventHandler(_target, handler);

		// public static EventProxy<TEventArgs> operator +(EventProxy<TEventArgs> left, EventHandler<TEventArgs> handler)
		// {
		// 	left._underlying.AddEventHandler(left._target, handler);
		// 	return left;
		// }
		//
		// public static EventProxy<TEventArgs> operator -(EventProxy<TEventArgs> left, EventHandler<TEventArgs> handler)
		// {
		// 	left._underlying.RemoveEventHandler(left._target, handler);
		// 	return left;
		// }

		[MoonSharpHidden]
		public void RaiseEvent(object sender, TEventArgs e) => (_target
															   .GetType()
															   .GetField(_underlying.Name,
																	BindingFlags.Instance | BindingFlags.NonPublic)
															  ?.GetValue(_target) as Delegate)
		  ?.DynamicInvoke(sender, e);
	}
}