// https://twitter.com/Cyanilux/status/1396848736022802435?s=20

#ifndef GRASS_INSTANCED_INCLUDED
#define GRASS_INSTANCED_INCLUDED

// ----------------------------------------------------------------------------------

// Graph should contain Boolean Keyword, "PROCEDURAL_INSTANCING_ON", Global, Multi-Compile.
// Must have two Custom Functions in vertex stage. One is used to attach this file (see Instancing_float below),
// and another to set #pragma instancing_options :

// It must use the String mode as this cannot be defined in includes.
// Without this, you will get "UNITY_INSTANCING_PROCEDURAL_FUNC must be defined" Shader Error.
/*
Out = In;
#pragma instancing_options procedural:vertInstancingSetup
*/
// I've found this works fine, but it might make sense for the pragma to be defined outside of a function,
// so could also use this slightly hacky method too
/*
Out = In;
}

#pragma instancing_options procedural:vertInstancingSetup

void dummy(){
*/

// ----------------------------------------------------------------------------------

StructuredBuffer<float4x4> matrixBuffer;
StructuredBuffer<float3> colorBuffer;
// Stores the matrices (and possibly other data) sent from the C# side via material.SetBuffer, in Start/OnEnable.
// See : https://gist.github.com/Cyanilux/e7afdc5c65094bfd0827467f8e4c3c54

#if UNITY_ANY_INSTANCING_ENABLED

	// Updates the unity_ObjectToWorld / unity_WorldToObject matrices so our matrix is taken into account

	// Based on : 
	// https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ParticlesInstancing.hlsl
	// and/or
	// https://github.com/TwoTailsGames/Unity-Built-in-Shaders/blob/master/CGIncludes/UnityStandardParticleInstancing.cginc

	void vertInstancingMatrices(inout float4x4 objectToWorld, out float4x4 worldToObject) {
		float4x4 data = matrixBuffer[unity_InstanceID];

		objectToWorld = mul(objectToWorld, data);

		// Transform matrix (override current)
		// I prefer keeping positions relative to the bounds passed into DrawMeshInstancedIndirect so use the above instead
		//objectToWorld._11_21_31_41 = float4(data.m._11_21_31, 0.0f);
		//objectToWorld._12_22_32_42 = float4(data.m._12_22_32, 0.0f);
		//objectToWorld._13_23_33_43 = float4(data.m._13_23_33, 0.0f);
		//objectToWorld._14_24_34_44 = float4(data.m._14_24_34, 1.0f);

		// Inverse transform matrix
		float3x3 w2oRotation;
		w2oRotation[0] = objectToWorld[1].yzx * objectToWorld[2].zxy - objectToWorld[1].zxy * objectToWorld[2].yzx;
		w2oRotation[1] = objectToWorld[0].zxy * objectToWorld[2].yzx - objectToWorld[0].yzx * objectToWorld[2].zxy;
		w2oRotation[2] = objectToWorld[0].yzx * objectToWorld[1].zxy - objectToWorld[0].zxy * objectToWorld[1].yzx;

		float det = dot(objectToWorld[0].xyz, w2oRotation[0]);
		w2oRotation = transpose(w2oRotation);
		w2oRotation *= rcp(det);
		float3 w2oPosition = mul(w2oRotation, -objectToWorld._14_24_34);

		worldToObject._11_21_31_41 = float4(w2oRotation._11_21_31, 0.0f);
		worldToObject._12_22_32_42 = float4(w2oRotation._12_22_32, 0.0f);
		worldToObject._13_23_33_43 = float4(w2oRotation._13_23_33, 0.0f);
		worldToObject._14_24_34_44 = float4(w2oPosition, 1.0f);
	}

	void vertInstancingSetup() {
		vertInstancingMatrices(unity_ObjectToWorld, unity_WorldToObject);
	}

#endif

// Shader Graph Functions

// Obtain InstanceID. e.g. Can be used as a Seed into Random Range node to generate random data per instance
void GetInstanceID_float(out float Out){
	Out = 0;
	#ifndef SHADERGRAPH_PREVIEW
	#if UNITY_ANY_INSTANCING_ENABLED
	Out = unity_InstanceID;
	#endif
	#endif
}

// Just passes the position through, allows us to actually attach this file to the graph.
// Should be placed somewhere in the vertex stage, e.g. right before connecting the object space position.
void InstancingPosition_float(float3 Position, out float3 Out){
	Out = Position;
}

void InstancingColor_float(float3 defaultColor, out float3 Out)
{
#if UNITY_ANY_INSTANCING_ENABLED
           Out = colorBuffer[unity_InstanceID];
#else
    Out = defaultColor;
#endif
}

#endif