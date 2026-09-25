shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
uniform bool hide = false;
uniform vec4 color : source_color = vec4(1.0);
uniform float strength : hint_range(0.0, 2.0) = 1.4;
uniform float gray_brightness : hint_range(0.0, 1.0) = 0.2;   // 0=完全变灰, 1=保持原色
uniform vec3 gray_tint : source_color = vec3(0.9, 0.9, 0.9);
uniform float brightness : hint_range(0.5, 3.0) = 0.9;         // 亮度系数，越大越亮

void fragment() {
    vec4 pixel = texture(SCREEN_TEXTURE, SCREEN_UV);
    COLOR = pixel;
    if (!hide) {
        vec3 grayscale_value = vec3(dot(pixel.rgb, vec3(0.299, 0.587, 0.114))) * gray_tint;
        float range = 1.0 - step(distance(pixel.rgb, color.rgb), strength);
        COLOR.rgb = mix(pixel.rgb, grayscale_value, range * (1.0 - gray_brightness));
        // 应用亮度系数
        COLOR.rgb *= brightness;
    }
}