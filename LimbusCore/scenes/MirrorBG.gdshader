shader_type canvas_item;
uniform float mirror_count = 3.0; 

void fragment() {
    vec2 uv = UV;

    float x_repeated = uv.x * mirror_count;

    float mirrored_x = abs(mod(x_repeated, 2.0) - 1.0);
    
    mirrored_x = clamp(mirrored_x, 0.001, 0.999);
    vec4 tex = texture(TEXTURE, vec2(mirrored_x, uv.y));
    COLOR = tex;
}