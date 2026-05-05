APP_STYLESHEET = """
QMainWindow, QWidget#missionRoot {
    background: #030711;
    color: #dbeafe;
    font-family: "Segoe UI";
    font-size: 9pt;
}

QFrame#missionHeader {
    background: #090d17;
    border-bottom: 1px solid #172033;
}

QLabel#eyebrowLabel {
    color: #64748b;
    font-size: 7pt;
    font-weight: 700;
    letter-spacing: 1px;
}

QLabel#titleLabel {
    color: #e2e8f0;
    font-size: 12pt;
    font-weight: 700;
}

QLabel#statusPill {
    color: #8ff6ff;
    background: #082836;
    border: 1px solid #14556a;
    border-radius: 7px;
    padding: 4px 10px;
    font-size: 8pt;
    font-weight: 600;
}

QFrame#sidePanel, QFrame#commandBar, QWidget#rightPanel {
    background: rgba(8, 15, 31, 235);
    border: 1px solid #172a46;
    border-radius: 8px;
}

QFrame#metricCard {
    background: #071225;
    border: 1px solid #15304f;
    border-radius: 8px;
}

QLabel#metricTitle {
    color: #64748b;
    font-size: 7pt;
    font-weight: 700;
}

QLabel#metricValue {
    color: #eff6ff;
    font-size: 11pt;
    font-weight: 700;
}

QLabel#metricHint {
    color: #94a3b8;
    font-size: 8pt;
}

QFrame#viewportShell {
    background: #02050d;
    border: 1px solid #152238;
    border-radius: 8px;
}

QLabel#viewportEmpty {
    color: #8ff6ff;
    background: rgba(3, 7, 18, 190);
    border: 1px solid #164e63;
    border-radius: 8px;
    padding: 18px 26px;
    font-size: 12pt;
    font-weight: 700;
}

QGroupBox {
    color: #cbd5e1;
    background: #071225;
    border: 1px solid #17304f;
    border-radius: 8px;
    margin-top: 15px;
    padding: 12px 10px 10px 10px;
    font-weight: 700;
}

QGroupBox::title {
    subcontrol-origin: margin;
    subcontrol-position: top left;
    left: 10px;
    padding: 0 5px;
    color: #7dd3fc;
    font-size: 8pt;
}

QLabel {
    color: #cbd5e1;
}

QComboBox, QDoubleSpinBox {
    color: #e2e8f0;
    background: #0b1730;
    border: 1px solid #203a5f;
    border-radius: 6px;
    padding: 5px 8px;
    min-height: 20px;
}

QComboBox:disabled, QDoubleSpinBox:disabled {
    color: #64748b;
    background: #07101f;
    border-color: #14243a;
}

QComboBox::drop-down {
    border: 0;
    width: 22px;
}

QPushButton {
    color: #dbeafe;
    background: #13223b;
    border: 1px solid #29476f;
    border-radius: 6px;
    padding: 6px 12px;
    font-weight: 600;
}

QPushButton:hover {
    background: #193154;
    border-color: #3b82f6;
}

QPushButton:pressed {
    background: #0ea5e9;
    color: #00111c;
}

QPushButton#primaryButton {
    background: #0e7490;
    border-color: #22d3ee;
    color: #ecfeff;
}

QPushButton#primaryButton:hover {
    background: #0891b2;
}

QCheckBox {
    color: #cbd5e1;
    spacing: 8px;
}

QCheckBox::indicator {
    width: 15px;
    height: 15px;
    border-radius: 4px;
    border: 1px solid #355279;
    background: #0b1730;
}

QCheckBox::indicator:checked {
    background: #22d3ee;
    border-color: #67e8f9;
}

QScrollArea {
    background: transparent;
    border: 0;
}

QScrollBar:vertical {
    background: #071225;
    width: 8px;
    margin: 2px;
}

QScrollBar::handle:vertical {
    background: #21466f;
    border-radius: 4px;
    min-height: 30px;
}

QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical {
    height: 0;
}

QStatusBar {
    color: #94a3b8;
    background: #050b17;
    border-top: 1px solid #172033;
}
"""
