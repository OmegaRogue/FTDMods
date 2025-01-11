using MoonSharp.Interpreter;

using UnityEngine;
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

namespace LuaExtension
{
	[MoonSharpUserData]
	public class LuaPID
	{
		public float Kp;
		public float Ki;
		public float Kd;
		public float Min;
		public float Max;

		public LuaPID(float kp, float ki, float kd, float min, float max,
					  float setPoint,
					  bool  ftdStyle = true)
		{
			Kp  = kp;
				Ki = ftdStyle?ki >0?kp /ki:ki:ki;
				Kd = ftdStyle?kp*kd:kd;
			
			Min       = min;
			Max       = max;
			SetPoint  = setPoint;
			_lastTime = Time.time;
		}


		public float Error      { get; private set; }
		public float Integral   { get; private set; }
		public float Derivative { get; private set; }
		public float LastError  { get; private set; }

		public float Value      { get; private set; }

		private float _lastTime;

		public float SetPoint;

		[MoonSharpUserDataMetamethod("__call")]
		public float Run(float measuredValue)
		{
			var dt = Time.time - _lastTime;
			Error     = SetPoint - measuredValue;
			var integral   = Integral +  Error               * dt;
			Derivative = (Error       - LastError) / dt;
			LastError  = Error;
			_lastTime  = Time.time;
			
			var value = Kp * Error + Ki * Integral + Kd * Derivative;

			if (value > Max)
			{
				if (integral <= Integral)
					Integral = integral;
				return Max;
			}
			if (value < Min)
			{
				if (integral >= Integral)
					Integral = integral;
				return Min;
			}
			Integral = integral;
			return Value;
		}
		
		public override string ToString() => $@"Error: {Error}
Integral: {Integral}
Derivative: {Derivative}
LastError: {LastError}
Min: {Min}
Max: {Max}
Value: {Value}
SetPoint: {SetPoint}";
	}
}