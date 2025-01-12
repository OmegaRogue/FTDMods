// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Unity.Mathematics;
using Unity.Mathematics.Geometry;


var luaCode      = @"function test(a,b) if a then return a end return a , b   , c end";


var node = new ConsoleApp1.DAG.FunctionNode(luaCode);

// Console.WriteLine(node);


var typeDict = new Dictionary<Type, string>
			   {
				   {typeof(bool), "boolean"},
				   {typeof(bool2), "bool2"},
				   {typeof(bool2x2), "bool2x2"},
				   {typeof(bool2x3), "bool2x3"},
				   {typeof(bool2x4), "bool2x4"},
				   {typeof(bool3), "bool3"},
				   {typeof(bool3x2), "bool3x2"},
				   {typeof(bool3x3), "bool3x3"},
				   {typeof(bool3x4), "bool3x4"},
				   {typeof(bool4), "bool4"},
				   {typeof(bool4x2), "bool4x2"},
				   {typeof(bool4x3), "bool4x3"},
				   {typeof(bool4x4), "bool4x4"},
				   {typeof(float), "number"},
				   {typeof(float2), "float2"},
				   {typeof(float2x2), "float2x2"},
				   {typeof(float2x3), "float2x3"},
				   {typeof(float2x4), "float2x4"},
				   {typeof(float3), "float3"},
				   {typeof(float3x2), "float3x2"},
				   {typeof(float3x3), "float3x3"},
				   {typeof(float3x4), "float3x4"},
				   {typeof(float4), "float4"},
				   {typeof(float4x2), "float4x2"},
				   {typeof(float4x3), "float4x3"},
				   {typeof(float4x4), "float4x4"},
				   {typeof(int), "integer"},
				   {typeof(int2), "int2"},
				   {typeof(int2x2), "int2x2"},
				   {typeof(int2x3), "int2x3"},
				   {typeof(int2x4), "int2x4"},
				   {typeof(int3), "int3"},
				   {typeof(int3x2), "int3x2"},
				   {typeof(int3x3), "int3x3"},
				   {typeof(int3x4), "int3x4"},
				   {typeof(int4), "int4"},
				   {typeof(int4x2), "int4x2"},
				   {typeof(int4x3), "int4x3"},
				   {typeof(int4x4), "int4x4"},
				   {typeof(uint), "integer"},
				   {typeof(uint2), "uint2"},
				   {typeof(uint2x2), "uint2x2"},
				   {typeof(uint2x3), "uint2x3"},
				   {typeof(uint2x4), "uint2x4"},
				   {typeof(uint3), "uint3"},
				   {typeof(uint3x2), "uint3x2"},
				   {typeof(uint3x3), "uint3x3"},
				   {typeof(uint3x4), "uint3x4"},
				   {typeof(uint4), "uint4"},
				   {typeof(uint4x2), "uint4x2"},
				   {typeof(uint4x3), "uint4x3"},
				   {typeof(uint4x4), "uint4x4"},
				   {typeof(double), "number"},
				   {typeof(double2), "double2"},
				   {typeof(double2x2), "double2x2"},
				   {typeof(double2x3), "double2x3"},
				   {typeof(double2x4), "double2x4"},
				   {typeof(double3), "double3"},
				   {typeof(double3x2), "double3x2"},
				   {typeof(double3x3), "double3x3"},
				   {typeof(double3x4), "double3x4"},
				   {typeof(double4), "double4"},
				   {typeof(double4x2), "double4x2"},
				   {typeof(double4x3), "double4x3"},
				   {typeof(double4x4), "double4x4"},
				   {typeof(half), "half"},
				   {typeof(half2), "half2"},
				   {typeof(half3), "half3"},
				   {typeof(half4), "half4"},
				   {typeof(quaternion), "quaternion"},
				   {typeof(RigidTransform), "RigidTransform"},
				   {typeof(MinMaxAABB), "MinMaxAABB"},
				   {typeof(string), "string"},
				   {typeof(void), ""},
				   {typeof(UnityEngine.Vector3), "Vector3"},
				   {typeof(Type), "Type"}
			   };


var luaOpDict = new Dictionary<string, string>
				{
					{"op_Equality", "__eq"},
					{"ToString", "__tostring"},
					{"op_Addition", "__add"},
					{"op_Subtraction", "__sub"},
					{"op_Multiply", "__mul"},
					{"op_Division", "__div"},
					{"op_Modulus", "__mod"},
					{"op_LessThan", "__mod"},
					{"op_LessThanOrEqual", "__mod"},
					{"op_UnaryNegation", "__unm"},
				};


string luaFields(Type type)
{
	var fields = type.GetFields();

	var sb = new StringBuilder();
	foreach (var field in fields)
	{
		if (!typeDict.TryGetValue(field.FieldType, out var value))
		{
			sb.Append(field.FieldType.Name);
			continue;
		}
		sb.Append($"---@field {field.Name} {value}");
		sb.Append('\n');
	}
	return sb.ToString();
}

IEnumerable<MethodInfo> GetExtensionMethods(Assembly assembly, Type extendedType)
{
	var isGenericTypeDefinition = extendedType.IsGenericType && extendedType.IsTypeDefinition;
	var query = from type in assembly.GetTypes()
				where type.IsSealed && !type.IsGenericType && !type.IsNested
				from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				where method.IsDefined(typeof(ExtensionAttribute), false)
				where isGenericTypeDefinition
						  ? method.GetParameters()[0].ParameterType.IsGenericType && method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == extendedType
						  : method.GetParameters()[0].ParameterType == extendedType
				select method;
	return query;
}
string luaOpsMeta(Type type)
{
	var methodSource = type.GetMethods().ToList();
	var thisAssembly = type.Assembly;
	methodSource.AddRange(GetExtensionMethods(thisAssembly, type));
	var methods = methodSource.Where(info => luaOpDict.ContainsKey(info.Name)&&info.DeclaringType != typeof(object)&&info.Name!="op_Implicit"&&info.Name!="op_Explicit");
	var sb      = new StringBuilder();
	foreach (var method in methods)
	{
		var retType    = typeDict[method.ReturnType];
		var parameters = method.GetParameters().Select(info => (info.Name, typeDict.TryGetValue(info.ParameterType, out var value), value)).ToArray();
		if(parameters.Any(tuple => !tuple.Item2))
			continue;
		// foreach (var valueTuple in parameters)
			// sb.Append($"---@param {valueTuple.Name} {valueTuple.value}\n");
		// sb.Append($"function {method.DeclaringType?.Name}{((method.Attributes & MethodAttributes.Static) != 0?".":":")}{method.Name}({string.Join(", ",parameters.Select(tuple => tuple.Name))}) end");
		if (!luaOpDict.TryGetValue(method.Name, out var op))
		{
			sb.Append('\n');
			sb.Append('\n');
			continue;
		}
		sb.Append($"---@operator {op[2..]}");
		if (parameters.Length > 0)
		{
			sb.Append('(');
			sb.Append($"{parameters.First().Name}: {parameters.First().value}");
			foreach (var valueTuple in parameters.Skip(1))
			{
				sb.Append($", {valueTuple.Name}: {valueTuple.value}");
			}
			sb.Append(')');
		}
		sb.Append($":{retType}");
		sb.Append('\n');
	}
	return sb.ToString();
}

string luaMethods(Type type)
{
	var methods = type.GetMethods().Where(info => !info.Name.Contains("op_")&&!info.Name.Contains("get_")&&!info.Name.Contains("set_")&&info.Name!="Equals"&&info.DeclaringType != typeof(object));
	var sb      = new StringBuilder();
	foreach (var method in methods)
	{
		if (luaOpDict.ContainsKey(method.Name))
			continue;
		var retType    = typeDict[method.ReturnType];
		var parameters = method.GetParameters().Select(info => (info.Name, typeDict.TryGetValue(info.ParameterType, out var value), value)).ToArray();
		if(parameters.Any(tuple => !tuple.Item2))
			continue;
		foreach (var valueTuple in parameters)
		{
			sb.Append($"---@param {valueTuple.Name} {valueTuple.value}\n");
		}
		sb.Append($"---@return {retType}\n");
		sb.Append($"function {method.DeclaringType?.Name}{((method.Attributes & MethodAttributes.Static) != 0?".":":")}{method.Name}({string.Join(", ",parameters.Select(tuple => tuple.Name))}) end");
		sb.Append('\n');
		sb.Append('\n');
	}
	sb.Append('\n');
	return sb.ToString();
}


string luaType(Type type) => $"---@class {type.Name}\n{luaFields(type)}{luaOpsMeta(type)}local {type.Name[..1].ToLower()+type.Name[1..]} = {{}}\n{luaMethods(type)}";

// Console.WriteLine(luaFields(typeof(bool2)));
Console.WriteLine(luaType(typeof(float3)));