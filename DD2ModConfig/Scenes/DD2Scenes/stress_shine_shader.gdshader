shader_type canvas_item;
render_mode unshaded;

// ----- 控制参数 -----
// 圆的半径（0~0.7，基于节点尺寸的比例）
uniform float radius : hint_range(0.0, 0.7) = 0.4;
// 边缘羽化宽度（仅用于抗锯齿，保持很小，如 0.01~0.02）
uniform float softness : hint_range(0.0, 0.5) = 0.12;
// 发光颜色（RGB 可超过 1.0 实现 HDR 高亮）
uniform vec4 color : source_color = vec4(1.0, 0.8, 0.4, 1.0);
// 整体亮度倍率（直接缩放颜色，用于动画变暗）
uniform float brightness : hint_range(0.0, 2.0) = 1.0;
// 【新增】整体透明度（0=完全透明，1=完全不透明）
uniform float global_alpha : hint_range(0.0, 1.0) = 0.3;

void fragment() {
    vec2 center = vec2(0.5);
    float dist = distance(UV, center);
    
    // 实心圆透明度 mask：dist < radius 时 mask 为 1，否则为 0
    float mask = 1.0 - smoothstep(radius - softness, radius + softness, dist);
    
    if (mask < 0.001) discard;
    
    // 最终透明度 = mask × global_alpha（颜色自身的 alpha 可独立控制）
    float final_alpha = mask * global_alpha * color.a;
    
    // 输出颜色（应用亮度倍率）
    COLOR = vec4(color.rgb * brightness, final_alpha);
}