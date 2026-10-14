shader_type spatial;
render_mode blend_mix, depth_draw_never, unshaded, cull_disabled;

uniform vec4 shadow_color : source_color = vec4(0.0, 0.0, 0.0, 0.3);
uniform float fade : hint_range(0.1, 2.0) = 2.0;

void fragment() {
    float d = distance(UV, vec2(0.5)) * 2.0;
    float alpha = (1.0 - smoothstep(0.0, fade, d)) * shadow_color.a;

    ALBEDO = shadow_color.rgb;
    ALPHA  = alpha;
}