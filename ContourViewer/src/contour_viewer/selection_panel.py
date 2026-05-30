from __future__ import annotations

from PySide6.QtCore import QSignalBlocker, QPointF, QRectF, Qt, Signal
from PySide6.QtGui import QColor, QPainter, QPainterPath, QPen
from PySide6.QtWidgets import (
    QCheckBox,
    QComboBox,
    QDoubleSpinBox,
    QFormLayout,
    QGroupBox,
    QLabel,
    QLineEdit,
    QPushButton,
    QScrollArea,
    QSizePolicy,
    QVBoxLayout,
    QWidget,
)

from .models import BeamData, LoadCaseInfo
from .section_profiles import beam_section_family, beam_section_name_text, beam_section_type_text


class SectionPreviewWidget(QWidget):
    def __init__(self, parent=None) -> None:
        super().__init__(parent)
        self._beam: BeamData | None = None
        self.setMinimumHeight(120)
        self.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Fixed)

    def set_beam(self, beam: BeamData | None) -> None:
        self._beam = beam
        self.update()

    def paintEvent(self, event) -> None:  # pragma: no cover - widget paint path
        painter = QPainter(self)
        painter.setRenderHint(QPainter.Antialiasing, True)
        rect = self.rect().adjusted(6, 6, -6, -6)
        painter.fillRect(rect, QColor("#081225"))
        painter.setPen(QPen(QColor("#1f3b5d"), 1.0))
        painter.drawRoundedRect(rect, 8, 8)

        if self._beam is None:
            painter.setPen(QPen(QColor("#64748b"), 1.0))
            painter.drawText(rect, Qt.AlignCenter, "Select a beam")
            return

        draw_rect = rect.adjusted(12, 12, -12, -28)
        painter.setPen(QPen(QColor("#8ff6ff"), 1.3))
        painter.setBrush(QColor("#0e7490"))

        family = beam_section_family(self._beam)
        if family in {"Wide Flange", "Tapered I"}:
            self._draw_i_section(painter, draw_rect, tapered=family == "Tapered I")
        elif family == "Channel":
            self._draw_channel_section(painter, draw_rect)
        elif family == "Angle":
            self._draw_angle_section(painter, draw_rect)
        elif family == "Double Angle":
            self._draw_double_angle_section(painter, draw_rect)
        elif family == "Tee":
            self._draw_tee_section(painter, draw_rect)
        elif family == "Tube":
            self._draw_tube_section(painter, draw_rect)
        elif family == "Pipe":
            self._draw_pipe_section(painter, draw_rect)
        elif family == "Tapered Tube":
            self._draw_tapered_tube_section(painter, draw_rect)
        elif family == "Circle":
            self._draw_circle_section(painter, draw_rect)
        elif family == "Trapezoid":
            self._draw_trapezoid_section(painter, draw_rect)
        else:
            self._draw_rect_section(painter, draw_rect)

        label_rect = QRectF(rect.left() + 8, rect.bottom() - 22, rect.width() - 16, 18)
        painter.setPen(QPen(QColor("#cbd5e1"), 1.0))
        painter.drawText(label_rect, Qt.AlignCenter, family)

    def _draw_rect_section(self, painter: QPainter, rect: QRectF) -> None:
        shape = QRectF(
            rect.left() + rect.width() * 0.18,
            rect.top() + rect.height() * 0.14,
            rect.width() * 0.64,
            rect.height() * 0.72,
        )
        painter.drawRoundedRect(shape, 4, 4)

    def _draw_i_section(self, painter: QPainter, rect: QRectF, tapered: bool) -> None:
        top_width = 0.68
        bottom_width = 0.68
        if tapered and self._beam is not None and len(self._beam.section_property_values) >= 6:
            values = self._beam.section_property_values
            max_width = max(values[3], values[5], 1.0)
            top_width = max(0.3, min(0.86, values[3] / max_width * 0.70))
            bottom_width = max(0.3, min(0.86, values[5] / max_width * 0.70))
        center_x = rect.left() + rect.width() * 0.5
        top = rect.top() + rect.height() * 0.08
        bottom = rect.bottom() - rect.height() * 0.08
        flange_h = rect.height() * 0.18
        web_w = rect.width() * 0.14
        top_rect = QRectF(center_x - rect.width() * top_width * 0.5, top, rect.width() * top_width, flange_h)
        bottom_rect = QRectF(center_x - rect.width() * bottom_width * 0.5, bottom - flange_h, rect.width() * bottom_width, flange_h)
        web_rect = QRectF(center_x - web_w * 0.5, top_rect.bottom(), web_w, bottom_rect.top() - top_rect.bottom())
        painter.drawRect(top_rect)
        painter.drawRect(bottom_rect)
        painter.drawRect(web_rect)

    def _draw_channel_section(self, painter: QPainter, rect: QRectF) -> None:
        x0 = rect.left() + rect.width() * 0.22
        y0 = rect.top() + rect.height() * 0.10
        w = rect.width() * 0.58
        h = rect.height() * 0.80
        t = min(w, h) * 0.16
        path = QPainterPath()
        path.addRect(QRectF(x0, y0, t, h))
        path.addRect(QRectF(x0, y0, w, t))
        path.addRect(QRectF(x0, y0 + h - t, w, t))
        painter.drawPath(path)
        painter.fillPath(path, QColor("#14b8a6"))

    def _draw_angle_section(self, painter: QPainter, rect: QRectF) -> None:
        x0 = rect.left() + rect.width() * 0.24
        y0 = rect.top() + rect.height() * 0.10
        w = rect.width() * 0.52
        h = rect.height() * 0.80
        t = min(w, h) * 0.18
        path = QPainterPath()
        path.addRect(QRectF(x0, y0, t, h))
        path.addRect(QRectF(x0, y0 + h - t, w, t))
        painter.drawPath(path)
        painter.fillPath(path, QColor("#14b8a6"))

    def _draw_double_angle_section(self, painter: QPainter, rect: QRectF) -> None:
        left_rect = QRectF(
            rect.left() + rect.width() * 0.14,
            rect.top() + rect.height() * 0.10,
            rect.width() * 0.26,
            rect.height() * 0.80,
        )
        right_rect = QRectF(
            rect.right() - rect.width() * 0.40,
            rect.top() + rect.height() * 0.10,
            rect.width() * 0.26,
            rect.height() * 0.80,
        )
        self._draw_angle_at(painter, left_rect, mirror=False)
        self._draw_angle_at(painter, right_rect, mirror=True)

    def _draw_angle_at(self, painter: QPainter, rect: QRectF, mirror: bool) -> None:
        t = min(rect.width(), rect.height()) * 0.22
        path = QPainterPath()
        if mirror:
            path.addRect(QRectF(rect.right() - t, rect.top(), t, rect.height()))
            path.addRect(QRectF(rect.left(), rect.bottom() - t, rect.width(), t))
        else:
            path.addRect(QRectF(rect.left(), rect.top(), t, rect.height()))
            path.addRect(QRectF(rect.left(), rect.bottom() - t, rect.width(), t))
        painter.drawPath(path)
        painter.fillPath(path, QColor("#14b8a6"))

    def _draw_tee_section(self, painter: QPainter, rect: QRectF) -> None:
        flange_rect = QRectF(
            rect.left() + rect.width() * 0.18,
            rect.top() + rect.height() * 0.10,
            rect.width() * 0.64,
            rect.height() * 0.20,
        )
        stem_rect = QRectF(
            rect.left() + rect.width() * 0.44,
            flange_rect.bottom(),
            rect.width() * 0.12,
            rect.height() * 0.58,
        )
        painter.drawRect(flange_rect)
        painter.drawRect(stem_rect)

    def _draw_tube_section(self, painter: QPainter, rect: QRectF) -> None:
        outer = QRectF(
            rect.left() + rect.width() * 0.18,
            rect.top() + rect.height() * 0.12,
            rect.width() * 0.64,
            rect.height() * 0.72,
        )
        inner = outer.adjusted(
            outer.width() * 0.20,
            outer.height() * 0.20,
            -outer.width() * 0.20,
            -outer.height() * 0.20,
        )
        path = QPainterPath()
        path.addRect(outer)
        inner_path = QPainterPath()
        inner_path.addRect(inner)
        path = path.subtracted(inner_path)
        painter.drawPath(path)
        painter.fillPath(path, QColor("#14b8a6"))

    def _draw_pipe_section(self, painter: QPainter, rect: QRectF) -> None:
        outer = QRectF(
            rect.left() + rect.width() * 0.18,
            rect.top() + rect.height() * 0.12,
            rect.width() * 0.64,
            rect.height() * 0.72,
        )
        inner = outer.adjusted(
            outer.width() * 0.20,
            outer.height() * 0.20,
            -outer.width() * 0.20,
            -outer.height() * 0.20,
        )
        path = QPainterPath()
        path.addEllipse(outer)
        inner_path = QPainterPath()
        inner_path.addEllipse(inner)
        path = path.subtracted(inner_path)
        painter.drawPath(path)
        painter.fillPath(path, QColor("#14b8a6"))

    def _draw_tapered_tube_section(self, painter: QPainter, rect: QRectF) -> None:
        outer = QPainterPath()
        outer.moveTo(rect.left() + rect.width() * 0.30, rect.top() + rect.height() * 0.10)
        outer.lineTo(rect.right() - rect.width() * 0.18, rect.top() + rect.height() * 0.18)
        outer.lineTo(rect.right() - rect.width() * 0.10, rect.bottom() - rect.height() * 0.14)
        outer.lineTo(rect.left() + rect.width() * 0.16, rect.bottom() - rect.height() * 0.22)
        outer.closeSubpath()
        inner = QPainterPath()
        inner.moveTo(rect.left() + rect.width() * 0.40, rect.top() + rect.height() * 0.24)
        inner.lineTo(rect.right() - rect.width() * 0.28, rect.top() + rect.height() * 0.28)
        inner.lineTo(rect.right() - rect.width() * 0.22, rect.bottom() - rect.height() * 0.24)
        inner.lineTo(rect.left() + rect.width() * 0.26, rect.bottom() - rect.height() * 0.28)
        inner.closeSubpath()
        path = outer.subtracted(inner)
        painter.drawPath(path)
        painter.fillPath(path, QColor("#14b8a6"))

    def _draw_circle_section(self, painter: QPainter, rect: QRectF) -> None:
        circle = QRectF(
            rect.left() + rect.width() * 0.20,
            rect.top() + rect.height() * 0.12,
            rect.width() * 0.60,
            rect.height() * 0.72,
        )
        painter.drawEllipse(circle)

    def _draw_trapezoid_section(self, painter: QPainter, rect: QRectF) -> None:
        points = [
            QPointF(rect.left() + rect.width() * 0.30, rect.top() + rect.height() * 0.12),
            QPointF(rect.right() - rect.width() * 0.30, rect.top() + rect.height() * 0.12),
            QPointF(rect.right() - rect.width() * 0.12, rect.bottom() - rect.height() * 0.12),
            QPointF(rect.left() + rect.width() * 0.12, rect.bottom() - rect.height() * 0.12),
        ]
        painter.drawPolygon(points)


class SelectionPanel(QWidget):
    up_axis_changed = Signal(str)
    extrude_toggled = Signal(bool)
    beam_color_mode_changed = Signal(str)
    isolate_toggled = Signal(bool)
    background_mode_changed = Signal(str)
    metric_layout_changed = Signal(bool)
    apply_requested = Signal()
    _LENGTH_TO_MM = {
        "MM": 1.0,
        "MILLIMETER": 1.0,
        "MILLIMETRE": 1.0,
        "CM": 10.0,
        "CENTIMETER": 10.0,
        "CENTIMETRE": 10.0,
        "M": 1000.0,
        "METER": 1000.0,
        "METRE": 1000.0,
        "IN": 25.4,
        "INCH": 25.4,
        "FT": 304.8,
        "FEET": 304.8,
        "FOOT": 304.8,
    }

    def __init__(self, parent=None) -> None:
        super().__init__(parent)
        self.setMinimumWidth(295)
        self.setMaximumWidth(310)
        self._build_ui()

    def _build_ui(self) -> None:
        root = QVBoxLayout(self)
        root.setContentsMargins(0, 0, 0, 0)
        root.setSpacing(0)

        scroll = QScrollArea(self)
        scroll.setWidgetResizable(True)
        scroll.setFrameShape(QScrollArea.NoFrame)
        scroll.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        content = QWidget(scroll)
        content.setSizePolicy(QSizePolicy.Ignored, QSizePolicy.Preferred)
        content_layout = QVBoxLayout(content)
        content_layout.setContentsMargins(8, 8, 8, 8)
        content_layout.setSpacing(8)

        self.view_box = QGroupBox("View", content)
        view_form = QFormLayout(self.view_box)
        view_form.setLabelAlignment(Qt.AlignLeft)
        view_form.setFormAlignment(Qt.AlignTop)
        view_form.setHorizontalSpacing(8)
        view_form.setVerticalSpacing(8)
        self.up_axis_combo = QComboBox(self.view_box)
        self.up_axis_combo.addItems(["Auto", "Y-up", "Z-up"])
        self.up_axis_combo.currentTextChanged.connect(self.up_axis_changed.emit)
        self.extrude_check = QCheckBox("Extrude", self.view_box)
        self.extrude_check.toggled.connect(self.extrude_toggled.emit)
        self.beam_color_combo = QComboBox(self.view_box)
        self.beam_color_combo.addItems(["Default", "Section Type"])
        self.beam_color_combo.currentTextChanged.connect(self.beam_color_mode_changed.emit)
        self.background_combo = QComboBox(self.view_box)
        self.background_combo.addItems(["Dark", "White"])
        self.background_combo.currentTextChanged.connect(self.background_mode_changed.emit)
        self.isolate_check = QCheckBox("Isolate selected beam", self.view_box)
        self.isolate_check.toggled.connect(self.isolate_toggled.emit)
        self.metric_left_check = QCheckBox("Metrics on left", self.view_box)
        self.metric_left_check.toggled.connect(self.metric_layout_changed.emit)
        view_form.addRow("Up Axis", self.up_axis_combo)
        view_form.addRow("", self.extrude_check)
        view_form.addRow("Beam Colors", self.beam_color_combo)
        view_form.addRow("Background", self.background_combo)
        view_form.addRow("", self.isolate_check)
        view_form.addRow("", self.metric_left_check)

        results_box = QGroupBox("Results", content)
        results_form = QFormLayout(results_box)
        results_form.setLabelAlignment(Qt.AlignLeft)
        results_form.setFormAlignment(Qt.AlignTop)
        results_form.setHorizontalSpacing(8)
        results_form.setVerticalSpacing(8)
        self.load_case_combo = QComboBox(results_box)
        self.result_component_combo = QComboBox(results_box)
        self.result_component_combo.addItem("Wireframe", "Wireframe")
        self.result_component_combo.addItem("DX (Global X)", "DX")
        self.result_component_combo.addItem("DY (Global Y)", "DY")
        self.result_component_combo.addItem("DZ (Global Z)", "DZ")
        self.result_component_combo.addItem("Pressure Intensity", "PRESSURE_INTENSITY")
        self.result_component_combo.currentIndexChanged.connect(self._update_pressure_intensity_visibility)
        self.scale_spin = QDoubleSpinBox(results_box)
        self.scale_spin.setRange(0.1, 10000.0)
        self.scale_spin.setDecimals(1)
        self.scale_spin.setValue(1.0)
        self.scale_spin.hide()
        self.non_display_members_edit = QLineEdit(results_box)
        self.non_display_members_edit.setPlaceholderText("101 102 TO 108")
        self.display_only_members_edit = QLineEdit(results_box)
        self.display_only_members_edit.setPlaceholderText("101 102 TO 108")
        self.query_only_check = QCheckBox("Query Only", results_box)
        self.displacement_unit_value = QLabel("-", results_box)
        self.results_state_value = QLabel("-", results_box)
        self.apply_button = QPushButton("Apply", results_box)
        self.apply_button.setObjectName("primaryButton")
        self.apply_button.clicked.connect(self.apply_requested.emit)
        results_form.addRow("Case", self.load_case_combo)
        results_form.addRow("Display", self.result_component_combo)
        results_form.addRow("Non-Display", self.non_display_members_edit)
        results_form.addRow("Display Only", self.display_only_members_edit)
        results_form.addRow("", self.query_only_check)
        results_form.addRow("Disp Unit", self.displacement_unit_value)
        results_form.addRow("State", self.results_state_value)
        results_form.addRow("", self.apply_button)

        self.intensity_box = QGroupBox("Pressure Intensity", content)
        intensity_form = QFormLayout(self.intensity_box)
        intensity_form.setLabelAlignment(Qt.AlignLeft)
        intensity_form.setFormAlignment(Qt.AlignTop)
        intensity_form.setHorizontalSpacing(8)
        intensity_form.setVerticalSpacing(8)
        self.intensity_nodes_edit = QLineEdit(self.intensity_box)
        self.intensity_nodes_edit.setPlaceholderText("101 102 103 TO 108")
        self.intensity_auto_elevation_check = QCheckBox("Auto from node group", self.intensity_box)
        self.intensity_auto_elevation_check.setChecked(True)
        self.intensity_auto_elevation_check.toggled.connect(self._update_intensity_elevation_enabled)
        self.intensity_elevation_spin = QDoubleSpinBox(self.intensity_box)
        self.intensity_elevation_spin.setRange(-1_000_000_000.0, 1_000_000_000.0)
        self.intensity_elevation_spin.setDecimals(4)
        self.intensity_elevation_spin.setValue(0.0)
        self.intensity_tolerance_spin = QDoubleSpinBox(self.intensity_box)
        self.intensity_tolerance_spin.setRange(0.0, 100.0)
        self.intensity_tolerance_spin.setDecimals(2)
        self.intensity_tolerance_spin.setSingleStep(0.5)
        self.intensity_tolerance_spin.setSuffix(" %")
        self.intensity_tolerance_spin.setValue(5.0)
        self.intensity_round_mm_spin = QDoubleSpinBox(self.intensity_box)
        self.intensity_round_mm_spin.setRange(0.0, 1_000_000.0)
        self.intensity_round_mm_spin.setDecimals(1)
        self.intensity_round_mm_spin.setSuffix(" mm")
        self.intensity_round_mm_spin.setValue(100.0)
        self.intensity_boundary_offset_spin = QDoubleSpinBox(self.intensity_box)
        self.intensity_boundary_offset_spin.setRange(0.0, 1_000_000.0)
        self.intensity_boundary_offset_spin.setDecimals(4)
        self.intensity_boundary_offset_spin.setValue(1.0)
        self.intensity_source_combo = QComboBox(self.intensity_box)
        self.intensity_source_combo.addItems(["Pressure / Area", "Support Reaction Force"])
        self.intensity_zone_mode_combo = QComboBox(self.intensity_box)
        self.intensity_zone_mode_combo.addItems(["Full Plane", "Column Zones"])
        self.intensity_state_value = QLabel("-", self.intensity_box)
        self.intensity_state_value.setWordWrap(True)
        intensity_form.addRow("Nodes", self.intensity_nodes_edit)
        intensity_form.addRow("Source", self.intensity_source_combo)
        intensity_form.addRow("Elevation", self.intensity_auto_elevation_check)
        intensity_form.addRow("Value", self.intensity_elevation_spin)
        intensity_form.addRow("Mean Tolerance", self.intensity_tolerance_spin)
        intensity_form.addRow("Round", self.intensity_round_mm_spin)
        intensity_form.addRow("Boundary Offset", self.intensity_boundary_offset_spin)
        intensity_form.addRow("Zone", self.intensity_zone_mode_combo)
        intensity_form.addRow("Status", self.intensity_state_value)

        self.contour_box = QGroupBox("Contour", content)
        contour_form = QFormLayout(self.contour_box)
        contour_form.setLabelAlignment(Qt.AlignLeft)
        contour_form.setFormAlignment(Qt.AlignTop)
        contour_form.setHorizontalSpacing(8)
        contour_form.setVerticalSpacing(8)
        self.auto_range_check = QCheckBox("Auto", self.contour_box)
        self.auto_range_check.setChecked(True)
        self.auto_range_check.toggled.connect(self._update_range_enabled)
        self.min_value_spin = QDoubleSpinBox(self.contour_box)
        self.min_value_spin.setRange(-1_000_000_000.0, 1_000_000_000.0)
        self.min_value_spin.setDecimals(4)
        self.max_value_spin = QDoubleSpinBox(self.contour_box)
        self.max_value_spin.setRange(-1_000_000_000.0, 1_000_000_000.0)
        self.max_value_spin.setDecimals(4)
        self.color_map_combo = QComboBox(self.contour_box)
        self.color_map_combo.addItems(["jet", "turbo", "viridis", "plasma", "coolwarm"])
        self.legend_combo = QComboBox(self.contour_box)
        self.legend_combo.addItems(["Vertical", "Horizontal"])
        self.legend_style_combo = QComboBox(self.contour_box)
        self.legend_style_combo.addItems(["App", "PyVista Default"])
        self.reference_combo = QComboBox(self.contour_box)
        self.reference_combo.addItems(["Global Absolute", "Relative to Support"])
        contour_form.addRow("Range", self.auto_range_check)
        contour_form.addRow("Min", self.min_value_spin)
        contour_form.addRow("Max", self.max_value_spin)
        contour_form.addRow("Colors", self.color_map_combo)
        contour_form.addRow("Legend", self.legend_combo)
        contour_form.addRow("Bar Style", self.legend_style_combo)
        contour_form.addRow("Reference", self.reference_combo)

        info_box = QGroupBox("Info", content)
        info_form = QFormLayout(info_box)
        info_form.setLabelAlignment(Qt.AlignLeft)
        info_form.setFormAlignment(Qt.AlignTop)
        info_form.setHorizontalSpacing(8)
        info_form.setVerticalSpacing(8)
        self.model_axis_value = QLabel("-", info_box)
        self.beam_id_value = QLabel("-", info_box)
        self.beam_joint_value = QLabel("-", info_box)
        self.beam_section_name_value = QLabel("-", info_box)
        self.beam_section_type_value = QLabel("-", info_box)
        self.beam_view_peak_value = QLabel("-", info_box)
        self.beam_global_peak_value = QLabel("-", info_box)
        self.beam_peak_pos_value = QLabel("-", info_box)
        self.joint_id_value = QLabel("-", info_box)
        self.joint_coord_value = QLabel("-", info_box)
        info_form.addRow("Model Up", self.model_axis_value)
        info_form.addRow("Beam ID", self.beam_id_value)
        info_form.addRow("Beam Joints", self.beam_joint_value)
        info_form.addRow("Section", self.beam_section_name_value)
        info_form.addRow("Type", self.beam_section_type_value)
        info_form.addRow("Beam View", self.beam_view_peak_value)
        info_form.addRow("Beam Global", self.beam_global_peak_value)
        info_form.addRow("Peak Pos", self.beam_peak_pos_value)
        info_form.addRow("Joint ID", self.joint_id_value)
        info_form.addRow("Joint XYZ", self.joint_coord_value)

        content_layout.addWidget(results_box)
        content_layout.addWidget(self.intensity_box)
        content_layout.addWidget(info_box)
        content_layout.addStretch(1)
        scroll.setWidget(content)
        root.addWidget(scroll)

        self.set_results_enabled(False)
        self._update_range_enabled(self.auto_range_check.isChecked())
        self._update_intensity_elevation_enabled(self.intensity_auto_elevation_check.isChecked())
        self._update_pressure_intensity_visibility()

    def _update_range_enabled(self, auto_enabled: bool) -> None:
        self._set_range_inputs_enabled(self.apply_button.isEnabled())

    def _set_range_inputs_enabled(self, enabled: bool) -> None:
        manual_enabled = enabled and not self.auto_range_check.isChecked()
        self.min_value_spin.setEnabled(manual_enabled)
        self.max_value_spin.setEnabled(manual_enabled)

    def _update_intensity_elevation_enabled(self, auto_enabled: bool) -> None:
        self.intensity_elevation_spin.setEnabled(not auto_enabled and self.apply_button.isEnabled())

    def _update_pressure_intensity_visibility(self) -> None:
        self.intensity_box.setVisible(self.selected_component() == "PRESSURE_INTENSITY")

    def clear_info(self) -> None:
        self.set_beam_info(None, None, None)
        self.set_beam_section(None)
        self.set_beam_result(None, None, None, None)
        self.set_joint_info(None, None)

    def set_extrude(self, enabled: bool) -> None:
        blocker = QSignalBlocker(self.extrude_check)
        self.extrude_check.setChecked(enabled)
        del blocker

    def set_model_up_axis(self, up_axis: str) -> None:
        self.model_axis_value.setText(f"{up_axis}-up")

    def set_load_cases(self, load_cases: list[LoadCaseInfo]) -> None:
        blocker = QSignalBlocker(self.load_case_combo)
        self.load_case_combo.clear()
        for load_case in load_cases:
            self.load_case_combo.addItem(load_case.display_name, load_case.case_id)
        if self.load_case_combo.count() > 0:
            self.load_case_combo.setCurrentIndex(0)
        del blocker

    def set_results_enabled(self, enabled: bool) -> None:
        self.load_case_combo.setEnabled(enabled)
        self.result_component_combo.setEnabled(enabled)
        self.scale_spin.setEnabled(enabled)
        self.non_display_members_edit.setEnabled(enabled)
        self.display_only_members_edit.setEnabled(enabled)
        self.query_only_check.setEnabled(enabled)
        self.auto_range_check.setEnabled(enabled)
        self.color_map_combo.setEnabled(enabled)
        self.legend_combo.setEnabled(enabled)
        self.legend_style_combo.setEnabled(enabled)
        self.reference_combo.setEnabled(enabled)
        self.intensity_nodes_edit.setEnabled(enabled)
        self.intensity_auto_elevation_check.setEnabled(enabled)
        self.intensity_tolerance_spin.setEnabled(enabled)
        self.intensity_round_mm_spin.setEnabled(enabled)
        self.intensity_boundary_offset_spin.setEnabled(enabled)
        self.intensity_source_combo.setEnabled(enabled)
        self.intensity_zone_mode_combo.setEnabled(enabled)
        self.background_combo.setEnabled(True)
        self.apply_button.setEnabled(enabled)
        self._set_range_inputs_enabled(enabled)
        self._update_intensity_elevation_enabled(self.intensity_auto_elevation_check.isChecked())

    def set_displacement_unit(self, unit: str | None) -> None:
        self.displacement_unit_value.setText(unit or "-")

    def set_results_state(self, text: str) -> None:
        self.results_state_value.setText(text)

    def set_selected_load_case(self, case_id: int | None) -> None:
        if case_id is None:
            return
        blocker = QSignalBlocker(self.load_case_combo)
        for index in range(self.load_case_combo.count()):
            if self.load_case_combo.itemData(index) == case_id:
                self.load_case_combo.setCurrentIndex(index)
                break
        del blocker

    def set_selected_component(self, component: str) -> None:
        blocker = QSignalBlocker(self.result_component_combo)
        for index in range(self.result_component_combo.count()):
            if self.result_component_combo.itemData(index) == component:
                self.result_component_combo.setCurrentIndex(index)
                break
        del blocker
        self._update_pressure_intensity_visibility()

    def selected_load_case_id(self) -> int | None:
        case_id = self.load_case_combo.currentData()
        return None if case_id is None else int(case_id)

    def selected_component(self) -> str:
        component = self.result_component_combo.currentData()
        return "Wireframe" if component is None else str(component)

    def selected_non_display_member_text(self) -> str:
        return self.non_display_members_edit.text().strip()

    def set_non_display_member_text(self, text: str) -> None:
        self.non_display_members_edit.setText(text)

    def selected_display_only_member_text(self) -> str:
        return self.display_only_members_edit.text().strip()

    def set_display_only_member_text(self, text: str) -> None:
        self.display_only_members_edit.setText(text)

    def is_query_only_enabled(self) -> bool:
        return self.query_only_check.isChecked()

    def set_query_only_enabled(self, enabled: bool) -> None:
        blocker = QSignalBlocker(self.query_only_check)
        self.query_only_check.setChecked(enabled)
        del blocker

    def selected_intensity_node_text(self) -> str:
        return self.intensity_nodes_edit.text().strip()

    def set_intensity_node_text(self, text: str) -> None:
        self.intensity_nodes_edit.setText(text)

    def is_intensity_elevation_auto(self) -> bool:
        return self.intensity_auto_elevation_check.isChecked()

    def set_intensity_elevation_auto(self, enabled: bool) -> None:
        blocker = QSignalBlocker(self.intensity_auto_elevation_check)
        self.intensity_auto_elevation_check.setChecked(enabled)
        del blocker
        self._update_intensity_elevation_enabled(enabled)

    def selected_intensity_elevation(self) -> float:
        return float(self.intensity_elevation_spin.value())

    def set_intensity_elevation(self, value: float) -> None:
        blocker = QSignalBlocker(self.intensity_elevation_spin)
        self.intensity_elevation_spin.setValue(float(value))
        del blocker

    def selected_intensity_tolerance(self) -> float:
        return float(self.intensity_tolerance_spin.value())

    def set_intensity_tolerance(self, value: float) -> None:
        blocker = QSignalBlocker(self.intensity_tolerance_spin)
        self.intensity_tolerance_spin.setValue(float(value))
        del blocker

    def selected_intensity_round_mm(self) -> float:
        return float(self.intensity_round_mm_spin.value())

    def set_intensity_round_mm(self, value: float) -> None:
        blocker = QSignalBlocker(self.intensity_round_mm_spin)
        self.intensity_round_mm_spin.setValue(float(value))
        del blocker

    def selected_intensity_boundary_offset(self) -> float:
        return float(self.intensity_boundary_offset_spin.value())

    def set_intensity_boundary_offset(self, value: float) -> None:
        blocker = QSignalBlocker(self.intensity_boundary_offset_spin)
        self.intensity_boundary_offset_spin.setValue(float(value))
        del blocker

    def selected_intensity_source_mode(self) -> str:
        return self.intensity_source_combo.currentText()

    def set_intensity_source_mode(self, mode: str) -> None:
        blocker = QSignalBlocker(self.intensity_source_combo)
        self.intensity_source_combo.setCurrentText(
            "Support Reaction Force" if mode == "Support Reaction Force" else "Pressure / Area"
        )
        del blocker

    def selected_intensity_zone_mode(self) -> str:
        return self.intensity_zone_mode_combo.currentText()

    def set_intensity_zone_mode(self, mode: str) -> None:
        blocker = QSignalBlocker(self.intensity_zone_mode_combo)
        self.intensity_zone_mode_combo.setCurrentText("Column Zones" if mode == "Column Zones" else "Full Plane")
        del blocker

    def set_intensity_state(self, text: str) -> None:
        self.intensity_state_value.setText(text)

    def selected_scale(self) -> float:
        return float(self.scale_spin.value())

    def is_auto_range(self) -> bool:
        return self.auto_range_check.isChecked()

    def contour_limits(self) -> tuple[float, float] | None:
        minimum = float(self.min_value_spin.value())
        maximum = float(self.max_value_spin.value())
        if minimum >= maximum:
            return None
        return minimum, maximum

    def selected_color_map(self) -> str:
        return self.color_map_combo.currentText()

    def selected_legend_orientation(self) -> str:
        return self.legend_combo.currentText()

    def selected_scalar_bar_style(self) -> str:
        return self.legend_style_combo.currentText()

    def selected_reference_mode(self) -> str:
        return self.reference_combo.currentText()

    def selected_beam_color_mode(self) -> str:
        return self.beam_color_combo.currentText()

    def selected_background_mode(self) -> str:
        return self.background_combo.currentText()

    def set_background_mode(self, mode: str) -> None:
        blocker = QSignalBlocker(self.background_combo)
        self.background_combo.setCurrentText("White" if str(mode).lower().startswith("w") else "Dark")
        del blocker

    def is_isolate_enabled(self) -> bool:
        return self.isolate_check.isChecked()

    def are_metrics_on_left_enabled(self) -> bool:
        return self.metric_left_check.isChecked()

    def set_metrics_on_left_enabled(self, enabled: bool) -> None:
        blocker = QSignalBlocker(self.metric_left_check)
        self.metric_left_check.setChecked(enabled)
        del blocker

    def set_color_map(self, color_map: str) -> None:
        blocker = QSignalBlocker(self.color_map_combo)
        self.color_map_combo.setCurrentText(color_map)
        del blocker

    def set_legend_orientation(self, orientation: str) -> None:
        blocker = QSignalBlocker(self.legend_combo)
        self.legend_combo.setCurrentText("Horizontal" if str(orientation).lower().startswith("h") else "Vertical")
        del blocker

    def set_scalar_bar_style(self, style: str) -> None:
        blocker = QSignalBlocker(self.legend_style_combo)
        self.legend_style_combo.setCurrentText(
            "PyVista Default" if str(style).strip().lower().startswith("pyvista") else "App"
        )
        del blocker

    def set_reference_mode(self, mode: str) -> None:
        blocker = QSignalBlocker(self.reference_combo)
        if mode == "Absolute":
            mode = "Global Absolute"
        self.reference_combo.setCurrentText(mode)
        del blocker

    def set_beam_color_mode(self, mode: str) -> None:
        blocker = QSignalBlocker(self.beam_color_combo)
        self.beam_color_combo.setCurrentText(mode)
        del blocker

    def set_isolate_enabled(self, enabled: bool) -> None:
        blocker = QSignalBlocker(self.isolate_check)
        self.isolate_check.setChecked(enabled)
        del blocker

    def set_auto_range(self, enabled: bool) -> None:
        blocker = QSignalBlocker(self.auto_range_check)
        self.auto_range_check.setChecked(enabled)
        del blocker
        self._set_range_inputs_enabled(self.apply_button.isEnabled())

    def set_contour_limits(self, minimum: float | None, maximum: float | None) -> None:
        if minimum is None or maximum is None:
            return
        min_blocker = QSignalBlocker(self.min_value_spin)
        max_blocker = QSignalBlocker(self.max_value_spin)
        self.min_value_spin.setValue(float(minimum))
        self.max_value_spin.setValue(float(maximum))
        del min_blocker
        del max_blocker

    def set_beam_info(self, beam_id: int | None, joint_a: int | None, joint_b: int | None) -> None:
        self.beam_id_value.setText("-" if beam_id is None else str(beam_id))
        if joint_a is None or joint_b is None:
            self.beam_joint_value.setText("-")
        else:
            self.beam_joint_value.setText(f"{joint_a} -> {joint_b}")

    def set_beam_section(self, beam: BeamData | None) -> None:
        self.beam_section_name_value.setText(beam_section_name_text(beam))
        self.beam_section_type_value.setText(beam_section_type_text(beam))

    def set_beam_result(
        self,
        display_value: float | None,
        global_value: float | None,
        position: float | None,
        unit: str | None,
    ) -> None:
        self.beam_view_peak_value.setText(self._format_displacement(display_value, unit))
        self.beam_global_peak_value.setText(self._format_displacement(global_value, unit))
        self.beam_peak_pos_value.setText("-" if position is None else f"{position:.3f}")

    def set_joint_info(self, joint_id: int | None, coords: tuple[float, float, float] | None) -> None:
        self.joint_id_value.setText("-" if joint_id is None else str(joint_id))
        if coords is None:
            self.joint_coord_value.setText("-")
        else:
            x, y, z = coords
            self.joint_coord_value.setText(f"{x:.3f}, {y:.3f}, {z:.3f}")

    def _format_displacement(self, value: float | None, unit: str | None) -> str:
        if value is None:
            return "-"

        suffix = f" {unit}" if unit else ""
        text = f"{value:.3f}{suffix}"
        converted = self._convert_to_mm(value, unit)
        if converted is not None and (unit or "").strip().upper() != "MM":
            text = f"{text} ({converted:.1f} mm)"
        return text

    def _convert_to_mm(self, value: float, unit: str | None) -> float | None:
        if unit is None:
            return None

        normalized = unit.strip().upper()
        factor = self._LENGTH_TO_MM.get(normalized)
        if factor is None:
            return None
        return value * factor
