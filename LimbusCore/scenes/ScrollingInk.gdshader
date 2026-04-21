shader_type canvas_item;

uniform float activity : hint_range(0.0, 1.0) = 0.0;
uniform float u_time = 0.0; 
uniform bool is_top = true;
uniform vec4 ink_color : source_color = vec4(1.996, 1.933, 1.682, 1.0);

uniform float act_min = 0.25;
uniform float act_max = 0.25;

float hash(vec2 p) {
   return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);
}

float noise(vec2 p) {
   vec2 i = floor(p);
   vec2 f = fract(p);
   f = f * f * (3.0 - 2.0 * f);
   return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
              mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

float domain_warp(vec2 p, float shimmer_t, float active_curve) {
   float strength = active_curve * 5.0;
   vec2 q = vec2(noise(p + vec2(0.0, shimmer_t * 2.0)), noise(p + vec2(5.2, 1.3)));
   vec2 r = vec2(noise(p + strength * q + vec2(1.7, 9.2)), noise(p + strength * q + vec2(8.3, 2.8)));
   return noise(p + strength * r);
}

void fragment() {
   float logic_input = clamp(activity, 0.0, 1.0);
   float effective_act = mix(act_min, act_max, logic_input);
   float active_curve = pow(effective_act, 0.35);
  
   // u_time is our smooth, accumulated odometer from C#
   float time = (u_time > 0.0) ? u_time : TIME * 0.35;
   float shimmer_time = TIME; 
  
   float dir = is_top ? 1.0 : -1.0;
   vec2 uv = UV;

   // 3. NOISE GENERATION
   vec2 warp_uv = vec2(uv.x * 0.75 + (time * dir), uv.y * 7.5);
   float n = domain_warp(warp_uv, shimmer_time, active_curve);
   float blob_noise = noise(vec2(uv.x * 25.0, shimmer_time * 2.0));
  
   // 4. EDGE & THICKNESS
   float thickness = mix(0.015, 0.075, active_curve * (n * 0.5 + blob_noise * 0.1));
   float base_edge = is_top ? 0.88 : 0.12;
   float dynamic_edge = base_edge + (n - 0.5) * 0.3;
  
   dynamic_edge = clamp(dynamic_edge, thickness + 0.005, 1.0 - thickness - 0.005);
  
   // 5. MASKING
   float dist = abs(uv.y - dynamic_edge);
   float line_mask = 1.0 - smoothstep(thickness - 0.003, thickness + 0.003, dist);
  
   bool is_behind = is_top ? (uv.y <= dynamic_edge) : (uv.y >= dynamic_edge);
  
   vec4 final_color = vec4(0.0);
   if (is_behind) {
       final_color = vec4(0.0, 0.0, 0.0, 1.0);
   }
  
   final_color = mix(final_color, ink_color, line_mask);
   COLOR = final_color;
}