shader_type canvas_item;

uniform sampler2D dissolve_noise_tex : source_color;
uniform float dissolve_value : hint_range(0.0, 1.0) = 0.0;
uniform float noise_rotation : hint_range(0.0, 6.28) = 0.0; 
uniform float ripple_speed : hint_range(0.0, 2.0) = 0.5;

void fragment() {
    // 1. Get the base ripple texture
    vec4 tex = texture(TEXTURE, UV);
    
    // 2. Rotating Noise Logic
    vec2 centered_uv = UV - vec2(0.5);
    float active_rotation = noise_rotation + (TIME * ripple_speed); 
    
    float cos_r = cos(active_rotation);
    float sin_r = sin(active_rotation);
    
    vec2 rotated_uv = vec2(
        centered_uv.x * cos_r - centered_uv.y * sin_r,
        centered_uv.x * sin_r + centered_uv.y * cos_r
    ) + vec2(0.5);
    
    // 3. Apply Dissolve
    float dissolve_noise = texture(dissolve_noise_tex, rotated_uv).r;
    float dissolve_mask = step(dissolve_value, dissolve_noise);
    
    // 4. Combine with modulate (COLOR) and the original alpha
    // Using Premultiplied Alpha for that clean glow
    float final_alpha = tex.a * COLOR.a * dissolve_mask;
    vec3 final_rgb = tex.rgb * COLOR.rgb;

    COLOR = vec4(final_rgb * final_alpha, final_alpha);
}