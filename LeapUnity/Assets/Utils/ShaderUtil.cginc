#ifndef SHADERUTIL_CGINC
#define SHADERUTIL_CGINC

inline float angleBetween(float4 v1, float4 v2)
{
	v1 = normalize(v1);
	v2 = normalize(v2);
	float angle = degrees(acos(dot(v1, v2)));

	return angle;
}

inline float4 projectPointOntoLineSegment(float4 p, float4 v1, float4 v2)
{
	float l = distance(v1, v2);
	if (l <= 0.001)
		return v1;

	float t = dot(p - v1, v2 - v1) / (l * l);
	if (t < 0)
		return v1;
	else if (t > 1)
		return v2;

	float4 pt = v1 + t * (v2 - v1);
	return pt;
}

inline float4 projectVectorOntoPlane(float4 n, float4 v)
{
	n = normalize(n);
	float4 vp = v - dot(v, n) * n;

	return vp;
}

#endif // SHADERUTIL_CGINC
