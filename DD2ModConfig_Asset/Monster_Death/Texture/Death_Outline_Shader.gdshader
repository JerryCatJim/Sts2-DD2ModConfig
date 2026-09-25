shader_type spatial;
render_mode unshaded, cull_front; // cull_front 是关键

void vertex() {
    VERTEX += NORMAL * 0.015; // 控制描边粗细
}

void fragment() {
    ALBEDO = vec3(0.0); // 纯黑
}