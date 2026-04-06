import codecs

with codecs.open('src/Eclipse.Controls/Controls.cs', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# 找到 Label 类中的 Measure 方法并修改
# 行号是从 0 开始的，所以要 -1
# 根据之前的信息：
# 418 行是 var scaledFontSize = ...
# 419 行是 var textWidth = ...
# 423 行是 _desiredSize = new Size(textWidth, lineHeight);

# 在 418 行后插入 scaledPadding
# 修改 423 行

# 先找到正确的行
for i, line in enumerate(lines):
    if 'var textWidth = context.MeasureText' in line and i > 400 and i < 430:
        print(f'Found textWidth at line {i+1}')
        # 在这一行前插入 scaledPadding
        insert_line = i
        new_line = '        var scaledPadding = Padding * context.Scale;\n'
        lines.insert(insert_line, new_line)
        break

# 再次查找 _desiredSize 行
for i, line in enumerate(lines):
    if '_desiredSize = new Size(textWidth, lineHeight)' in line and i > 410 and i < 435:
        print(f'Found _desiredSize at line {i+1}')
        lines[i] = '        _desiredSize = new Size(textWidth + scaledPadding * 2, lineHeight + scaledPadding * 2);\n'
        break

with codecs.open('src/Eclipse.Controls/Controls.cs', 'w', encoding='utf-8') as f:
    f.writelines(lines)

print('Done')