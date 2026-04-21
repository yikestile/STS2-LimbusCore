shader_type canvas_item;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    vec3 base = tex.rgb;
    vec3 blend = COLOR.rgb;
    
    // 1. RIM SHADOW LOGIC
    // Calculate distance from center (0.5, 0.5)
    float dist = distance(UV, vec2(0.5));
    // Create a shadow mask that gets darker toward the edges
    float rim = 1.0 - smoothstep(0.2, 0.5, dist);
    
    // 2. BLEND MODES
    float luma_blend = (blend.r + blend.g + blend.b) / 3.0;
    
    // Overlay for contrast
    vec3 overlay;
    for(int i=0; i<3; i++){
        overlay[i] = base[i] < 0.5 ? (2.0 * base[i] * blend[i]) : (1.0 - 2.0 * (1.0 - base[i]) * (1.0 - blend[i]));
    }
    
    // Screen for glow
    vec3 screen = 1.0 - (1.0 - base) * (1.0 - blend);
    
    // Mix based on SP brightness
    vec3 final_rgb = mix(overlay, screen, smoothstep(0.4, 0.7, luma_blend));
    
    // 3. APPLY RIM SHADOW
    // This "crushes" the edges to add depth like Image 2/3
    final_rgb *= mix(0.4, 1.0, rim);

    COLOR = vec4(final_rgb, tex.a * COLOR.a);
}