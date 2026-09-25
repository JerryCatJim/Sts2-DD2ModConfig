shader_type canvas_item;

uniform vec2 rect_min = vec2(0, 0.89);
uniform vec2 rect_max = vec2(1, 1);

void fragment() {
    vec4 col = texture(TEXTURE, UV);
    // 如果当前像素在矩形区域内，把 Alpha 变成 0（透明）
    if (UV.x >= rect_min.x && UV.x <= rect_max.x &&
        UV.y >= rect_min.y && UV.y <= rect_max.y) {
        col.a = 0.0;
    }
    COLOR = col;
}