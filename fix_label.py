import re

with open('src/Eclipse.Controls/Controls.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# 替换 Label.Measure 中的内容
old_text = '''        var scaledFontSize = FontSize * context.Scale;
        var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
        
        // 行高通常是字体大小的 1.2-1.5 倍
        var lineHeight = scaledFontSize * 1.3;
        
        _desiredSize = new Size(textWidth, lineHeight);'''

new_text = '''        var scaledFontSize = FontSize * context.Scale;
        var scaledPadding = Padding * context.Scale;
        var textWidth = context.MeasureText(Text, scaledFontSize, FontFamily);
        
        // 行高通常是字体大小的 1.2-1.5 倍
        var lineHeight = scaledFontSize * 1.3;
        
        // 考虑 Padding
        _desiredSize = new Size(textWidth + scaledPadding * 2, lineHeight + scaledPadding * 2);'''

# 只替换 Label 类中的这段代码（通过查找 Label 类定义后的第一个匹配）
label_start = content.find('public class Label : ComponentBase')
if label_start != -1:
    # 在 Label 类中查找
    label_section = content[label_start:]
    
    # 找到下一个 class 定义的位置
    next_class = label_section.find('\npublic class', 10)
    if next_class != -1:
        label_section_end = label_start + next_class
        label_content = content[label_start:label_section_end]
        
        # 替换
        if old_text in label_content:
            new_label_content = label_content.replace(old_text, new_text)
            content = content[:label_start] + new_label_content + content[label_section_end:]
            
            with open('src/Eclipse.Controls/Controls.cs', 'w', encoding='utf-8') as f:
                f.write(content)
            print('Done - Label.Measure updated with Padding')
        else:
            print('Old text not found in Label class')
    else:
        print('Could not find end of Label class')
else:
    print('Label class not found')