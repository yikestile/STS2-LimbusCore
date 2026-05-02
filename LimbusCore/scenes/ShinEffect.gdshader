shader_type canvas_item;
render_mode blend_add;

// --- TEXTURES ---
uniform sampler2D main_tex : source_color, filter_linear, repeat_disable; 
uniform sampler2D noise_03 : repeat_enable; 
uniform sampler2D noise_04 : repeat_enable; 
uniform sampler2D thread_noise_tex : repeat_enable;         

// --- CONTROLS ---
uniform vec2 noise_03_speed = vec2(0.05, -0.6);   
uniform vec2 noise_04_speed = vec2(-0.1, -0.4); 
uniform vec2 thread_speed = vec2(0.0, 0.5);
uniform float pulse_speed : hint_range(0.0, 5.0) = 1.2;
uniform vec2 thread_tiling = vec2(1.0, 1.5);
uniform float thread_intensity = 3.869;

void fragment() {
    float breathing_strength = 0.75 + (sin(TIME * pulse_speed) * 0.25);
    
    // 1. SAMPLE NOISE 03 (Primary Shape)
    vec2 uv_03 = UV + (TIME * noise_03_speed);
    float map_03 = texture(noise_03, uv_03).r;
    
    // 2. COUNTER-ROTATING NOISE 04 (The Fix)
    // Sample A: Normal flow
    vec2 uv_04_a = UV + (TIME * noise_04_speed);
    float map_04_a = textureLod(noise_04, uv_04_a, 2.0).r;
    
    // Sample B: Flipped UVs (Counter-aligned) but same speed direction
    // 1.0 - UV effectively rotates the texture 180 degrees
    vec2 uv_04_b = (1.0 - UV) + (TIME * noise_04_speed);
    float map_04_b = textureLod(noise_04, uv_04_b, 2.0).r;
    
    // Mix the two noise04 samples to cancel out the "diagonal bias"
    float detail_noise = (map_04_a + map_04_b) * 0.5;
    
    // 3. BLEND WITH STABLE NOISE
    float combined_map = (map_03 * 0.5) + (detail_noise * 0.5);
    
    // 4. APPLY DISTORTION
    vec2 main_uv = UV;
    main_uv.x += (combined_map - 0.5) * breathing_strength;
    main_uv.y += (combined_map - 0.5) * 0.15; 
    
    // 5. SAMPLE MAIN BODY
    vec4 main_body = textureLod(main_tex, main_uv, 0.5);
    
    // 6. RISING THREADS
    vec2 t_uv = (UV * thread_tiling) + (TIME * thread_speed);
    float thread_map = texture(thread_noise_tex, t_uv).r;
    float thread_mask = smoothstep(0.4, 0.6, thread_map * main_body.r);
    
    // 7. COMPOSITE
    vec3 threads = vec3(thread_mask * thread_intensity) * COLOR.rgb;
    vec3 final_rgb = (main_body.rgb * COLOR.rgb) + threads;
    
    COLOR = vec4(final_rgb, main_body.a * COLOR.a);
}