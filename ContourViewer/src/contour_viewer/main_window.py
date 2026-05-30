from PySide6.QtCore import QSettings, Qt
from PySide6.QtGui import QAction
from PySide6.QtWidgets import (
    QFrame,
    QGridLayout,
    QHBoxLayout,
    QLabel,
    QDialog,
    QMainWindow,
    QMessageBox,
    QPushButton,
    QSizePolicy,
    QStatusBar,
    QVBoxLayout,
    QWidget,
)
from dataclasses import replace

from .models import BeamData, HorizontalPressureIntensityField, ModelGeometry, ModelResultField, NodeData
from .selection_panel import SelectionPanel
from .staad_connector import ResultMetadata, StaadConnectionError, StaadConnector
from .viewport import Viewport3D


class MainWindow(QMainWindow):
    def __init__(self) -> None:
        super().__init__()
        self.setWindowTitle("ContourViewer")
        self.resize(1200, 800)
        self._connector = StaadConnector()
        self._current_model: ModelGeometry | None = None
        self._current_result_metadata = ResultMetadata(results_available=False, displacement_unit=None)
        self._current_result_field: ModelResultField | HorizontalPressureIntensityField | None = None
        self._current_component = "Wireframe"
        self._current_load_case_id: int | None = None
        self._deformation_scale = 1.0
        self._auto_contour_range = True
        self._manual_scalar_limits: tuple[float, float] | None = None
        self._color_map = "jet"
        self._legend_orientation = "Vertical"
        self._scalar_bar_style = "App"
        self._reference_mode = "Global Absolute"
        self._non_display_member_text = ""
        self._non_display_member_ids: set[int] = set()
        self._display_only_member_text = ""
        self._display_only_member_ids: set[int] = set()
        self._query_member_only = False
        self._intensity_node_text = ""
        self._intensity_elevation_auto = True
        self._intensity_elevation = 0.0
        self._intensity_tolerance = 5.0
        self._intensity_round_mm = 100.0
        self._intensity_boundary_offset = 1.0
        self._intensity_source_mode = "Pressure / Area"
        self._intensity_zone_mode = "Full Plane"
        self._grid_visible = True
        self._show_extrude = False
        self._beam_color_mode = "Default"
        self._background_mode = "Dark"
        self._isolate_selection = False
        self._metric_cards_on_left = False
        self._settings = QSettings()
        self._result_cache: dict[tuple[object, ...], ModelResultField | HorizontalPressureIntensityField] = {}
        self._model_metric_labels: dict[str, QLabel] = {}

        self._load_persisted_settings()
        self._create_actions()
        self._create_central_widget()
        self._create_status_bar()
        self._apply_persisted_ui_state()

    def _create_actions(self) -> None:
        self.connect_action = QAction("Connect STAAD", self)
        self.connect_action.setStatusTip("Attach to the active STAAD session")
        self.connect_action.triggered.connect(self._connect_and_load_geometry)

        self.reload_action = QAction("Reload", self)
        self.reload_action.setStatusTip("Reload nodes and beams from STAAD")
        self.reload_action.triggered.connect(self._reload_geometry)

        self.toggle_joints_action = QAction("Joints On/Off", self)
        self.toggle_joints_action.triggered.connect(self._toggle_joint_visibility)

        self.hide_grid_action = QAction("Hide Grid", self)
        self.hide_grid_action.triggered.connect(lambda: self._set_grid_visibility(False))

        self.show_grid_action = QAction("Unhide Grid", self)
        self.show_grid_action.triggered.connect(lambda: self._set_grid_visibility(True))

    def _create_central_widget(self) -> None:
        self._model_metric_labels.clear()
        root = QWidget(self)
        root.setObjectName("missionRoot")
        layout = QVBoxLayout(root)
        layout.setContentsMargins(16, 12, 16, 10)
        layout.setSpacing(10)

        layout.addWidget(self._create_header(root))

        body = QHBoxLayout()
        body.setContentsMargins(0, 0, 0, 0)
        body.setSpacing(10)
        if self._metric_cards_on_left:
            body.addWidget(self._create_model_panel(root), 0)

        viewport_shell = QFrame(root)
        viewport_shell.setObjectName("viewportShell")
        viewport_shell.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        viewport_layout = QGridLayout(viewport_shell)
        viewport_layout.setContentsMargins(1, 1, 1, 1)
        viewport_layout.setSpacing(0)

        self.viewport = Viewport3D(viewport_shell)
        self.viewport.set_selection_callbacks(
            on_beam_selected=self._handle_beam_selected,
            on_joint_selected=self._handle_joint_selected,
        )
        self.viewport.set_grid_visibility(self._grid_visible, rerender=False)
        self.viewport.set_background_mode(self._background_mode, rerender=False)
        self.viewport.show_placeholder()
        viewport_layout.addWidget(self.viewport, 0, 0)
        self._empty_viewport_label = QLabel("Connect to STAAD\nLoad geometry to begin field review", viewport_shell)
        self._empty_viewport_label.setObjectName("viewportEmpty")
        self._empty_viewport_label.setAlignment(Qt.AlignCenter)
        viewport_layout.addWidget(self._empty_viewport_label, 0, 0, Qt.AlignCenter)
        body.addWidget(viewport_shell, 1)

        self.selection_panel = SelectionPanel(root)
        self.selection_panel.setObjectName("rightPanel")
        self.selection_panel.up_axis_changed.connect(self._change_up_axis)
        self.selection_panel.extrude_toggled.connect(self._set_extrude_mode)
        self.selection_panel.beam_color_mode_changed.connect(self._set_beam_color_mode)
        self.selection_panel.background_mode_changed.connect(self._set_background_mode)
        self.selection_panel.isolate_toggled.connect(self._set_isolate_selected_beam)
        self.selection_panel.metric_layout_changed.connect(self._set_metric_cards_on_left)
        self.selection_panel.apply_requested.connect(self._apply_selected_view)
        self.selection_panel.set_background_mode(self._background_mode)
        self.selection_panel.set_metrics_on_left_enabled(self._metric_cards_on_left)
        self.selection_panel.set_scalar_bar_style(self._scalar_bar_style)
        self.viewport.set_scalar_bar_style(self._scalar_bar_style, rerender=False)
        self._create_contour_settings_dialog()
        self._create_view_settings_dialog()
        body.addWidget(self.selection_panel, 0)

        layout.addLayout(body, 1)
        layout.addWidget(self._create_command_bar(root), 0)
        self.setCentralWidget(root)
        self._update_model_summary()

    def _create_header(self, parent: QWidget) -> QFrame:
        header = QFrame(parent)
        header.setObjectName("missionHeader")
        header.setFixedHeight(48 if self._metric_cards_on_left else 86)
        layout = QHBoxLayout(header)
        layout.setContentsMargins(10, 8, 10, 8)
        layout.setSpacing(8)

        if self._metric_cards_on_left:
            title_box = QVBoxLayout()
            title_box.setContentsMargins(0, 0, 0, 0)
            title_box.setSpacing(0)
            eyebrow = QLabel("STAAD RESULT VISUALIZER", header)
            eyebrow.setObjectName("eyebrowLabel")
            title = QLabel("ContourViewer / Structural Field Review", header)
            title.setObjectName("titleLabel")
            title_box.addWidget(eyebrow)
            title_box.addWidget(title)
            layout.addLayout(title_box, 1)
        else:
            for key, title, value, hint in (
                ("model", "MODEL", "No model", "Connect STAAD"),
                ("nodes", "NODES", "-", "Geometry"),
                ("beams", "BEAMS", "-", "Members"),
                ("cases", "LOAD CASES", "-", "Analysis"),
                ("unit", "UNIT", "-", "Length"),
                ("selection", "SELECTION", "-", "Pick beam/joint"),
            ):
                card = self._create_metric_card(header, key, title, value, hint)
                card.setFixedWidth(190)
                layout.addWidget(card, 0)
            layout.addStretch(1)

        return header

    def _create_model_panel(self, parent: QWidget) -> QFrame:
        panel = QFrame(parent)
        panel.setObjectName("sidePanel")
        panel.setFixedWidth(190)
        layout = QVBoxLayout(panel)
        layout.setContentsMargins(10, 10, 10, 10)
        layout.setSpacing(8)

        for key, title, value, hint in (
            ("model", "MODEL", "No model", "Connect STAAD"),
            ("nodes", "NODES", "-", "Geometry"),
            ("beams", "BEAMS", "-", "Members"),
            ("cases", "LOAD CASES", "-", "Analysis"),
            ("unit", "UNIT", "-", "Length"),
            ("selection", "SELECTION", "-", "Pick beam/joint"),
        ):
            layout.addWidget(self._create_metric_card(panel, key, title, value, hint))
        layout.addStretch(1)
        return panel

    def _create_metric_card(
        self,
        parent: QWidget,
        key: str,
        title: str,
        value: str,
        hint: str,
    ) -> QFrame:
        card = QFrame(parent)
        card.setObjectName("metricCard")
        card.setMinimumHeight(64)
        layout = QVBoxLayout(card)
        layout.setContentsMargins(10, 8, 10, 8)
        layout.setSpacing(2)

        title_label = QLabel(title, card)
        title_label.setObjectName("metricTitle")
        value_label = QLabel(value, card)
        value_label.setObjectName("metricValue")
        value_label.setWordWrap(True)
        hint_label = QLabel(hint, card)
        hint_label.setObjectName("metricHint")
        hint_label.setWordWrap(True)
        layout.addWidget(title_label)
        layout.addWidget(value_label)
        layout.addWidget(hint_label)
        self._model_metric_labels[key] = value_label
        return card

    def _create_command_bar(self, parent: QWidget) -> QFrame:
        bar = QFrame(parent)
        bar.setObjectName("commandBar")
        bar.setFixedHeight(58)
        layout = QHBoxLayout(bar)
        layout.setContentsMargins(12, 8, 12, 8)
        layout.setSpacing(8)

        connect_button = QPushButton("Connect STAAD", bar)
        connect_button.setObjectName("primaryButton")
        connect_button.clicked.connect(self._connect_and_load_geometry)
        reload_button = QPushButton("Reload", bar)
        reload_button.clicked.connect(self._reload_geometry)
        joints_button = QPushButton("Joints On/Off", bar)
        joints_button.clicked.connect(self._toggle_joint_visibility)
        grid_button = QPushButton("Grid On/Off", bar)
        grid_button.clicked.connect(lambda: self._set_grid_visibility(not self.viewport.is_grid_visible()))
        contour_button = QPushButton("Contour Settings", bar)
        contour_button.clicked.connect(self._show_contour_settings)
        view_button = QPushButton("View Settings", bar)
        view_button.clicked.connect(self._show_view_settings)

        layout.addWidget(connect_button)
        layout.addWidget(reload_button)
        layout.addSpacing(8)
        layout.addWidget(joints_button)
        layout.addWidget(grid_button)
        layout.addWidget(contour_button)
        layout.addWidget(view_button)
        layout.addStretch(1)
        self._mode_label = QLabel("WIRE / READY", bar)
        self._mode_label.setObjectName("metricHint")
        self._mode_label.setAlignment(Qt.AlignRight | Qt.AlignVCenter)
        layout.addWidget(self._mode_label)
        return bar

    def _create_contour_settings_dialog(self) -> None:
        self._contour_settings_dialog = QDialog(self)
        self._contour_settings_dialog.setWindowTitle("Contour Settings")
        self._contour_settings_dialog.setModal(False)
        layout = QVBoxLayout(self._contour_settings_dialog)
        layout.setContentsMargins(10, 10, 10, 10)
        layout.setSpacing(8)
        self.selection_panel.contour_box.setParent(self._contour_settings_dialog)
        self.selection_panel.contour_box.setMinimumWidth(320)
        self.selection_panel.contour_box.setMaximumWidth(520)
        layout.addWidget(self.selection_panel.contour_box)

    def _show_contour_settings(self) -> None:
        if not hasattr(self, "_contour_settings_dialog"):
            self._create_contour_settings_dialog()
        self._contour_settings_dialog.show()
        self._contour_settings_dialog.raise_()
        self._contour_settings_dialog.activateWindow()

    def _create_view_settings_dialog(self) -> None:
        self._view_settings_dialog = QDialog(self)
        self._view_settings_dialog.setWindowTitle("View Settings")
        self._view_settings_dialog.setModal(False)
        layout = QVBoxLayout(self._view_settings_dialog)
        layout.setContentsMargins(10, 10, 10, 10)
        layout.setSpacing(8)
        self.selection_panel.view_box.setParent(self._view_settings_dialog)
        self.selection_panel.view_box.setMinimumWidth(320)
        self.selection_panel.view_box.setMaximumWidth(520)
        layout.addWidget(self.selection_panel.view_box)

    def _show_view_settings(self) -> None:
        if not hasattr(self, "_view_settings_dialog"):
            self._create_view_settings_dialog()
        self._view_settings_dialog.show()
        self._view_settings_dialog.raise_()
        self._view_settings_dialog.activateWindow()

    def _set_metric_cards_on_left(self, enabled: bool) -> None:
        enabled = bool(enabled)
        if self._metric_cards_on_left == enabled:
            return

        self._capture_panel_state()
        self._metric_cards_on_left = enabled
        self._close_settings_dialogs()
        self._create_central_widget()
        self._restore_current_view()
        self._save_persisted_settings()
        location = "left panel" if enabled else "top header"
        self.statusBar().showMessage(f"Metric cards moved to {location}.", 3000)

    def _capture_panel_state(self) -> None:
        if not hasattr(self, "selection_panel"):
            return

        self._saved_up_axis_text = self.selection_panel.up_axis_combo.currentText()
        self._current_load_case_id = self.selection_panel.selected_load_case_id()
        self._current_component = self.selection_panel.selected_component()
        self._color_map = self.selection_panel.selected_color_map()
        self._legend_orientation = self.selection_panel.selected_legend_orientation()
        self._scalar_bar_style = self.selection_panel.selected_scalar_bar_style()
        self._reference_mode = self.selection_panel.selected_reference_mode()
        self._non_display_member_text = self.selection_panel.selected_non_display_member_text()
        try:
            self._non_display_member_ids = set(self._parse_member_group(self._non_display_member_text))
        except ValueError:
            self._non_display_member_ids = set()
        self._display_only_member_text = self.selection_panel.selected_display_only_member_text()
        try:
            self._display_only_member_ids = set(self._parse_member_group(self._display_only_member_text))
        except ValueError:
            self._display_only_member_ids = set()
        self._query_member_only = self.selection_panel.is_query_only_enabled()
        self._intensity_node_text = self.selection_panel.selected_intensity_node_text()
        self._intensity_elevation_auto = self.selection_panel.is_intensity_elevation_auto()
        self._intensity_elevation = self.selection_panel.selected_intensity_elevation()
        self._intensity_tolerance = self.selection_panel.selected_intensity_tolerance()
        self._intensity_round_mm = self.selection_panel.selected_intensity_round_mm()
        self._intensity_boundary_offset = self.selection_panel.selected_intensity_boundary_offset()
        self._intensity_source_mode = self.selection_panel.selected_intensity_source_mode()
        self._intensity_zone_mode = self.selection_panel.selected_intensity_zone_mode()
        self._auto_contour_range = self.selection_panel.is_auto_range()
        self._manual_scalar_limits = None if self._auto_contour_range else self.selection_panel.contour_limits()
        self._show_extrude = self.selection_panel.extrude_check.isChecked()
        self._beam_color_mode = self.selection_panel.selected_beam_color_mode()
        self._background_mode = self.selection_panel.selected_background_mode()
        self._isolate_selection = self.selection_panel.is_isolate_enabled()

    def _close_settings_dialogs(self) -> None:
        for attr in ("_contour_settings_dialog", "_view_settings_dialog"):
            dialog = getattr(self, attr, None)
            if dialog is not None:
                dialog.close()
                dialog.deleteLater()
                delattr(self, attr)

    def _restore_current_view(self) -> None:
        self.selection_panel.set_background_mode(self._background_mode)
        self.selection_panel.set_metrics_on_left_enabled(self._metric_cards_on_left)
        self.viewport.set_scalar_bar_style(self._scalar_bar_style, rerender=False)
        self.selection_panel.set_color_map(self._color_map)
        self.selection_panel.set_legend_orientation(self._legend_orientation)
        self.selection_panel.set_scalar_bar_style(self._scalar_bar_style)
        self.selection_panel.set_reference_mode(self._reference_mode)
        self.selection_panel.set_non_display_member_text(self._non_display_member_text)
        self.selection_panel.set_display_only_member_text(self._display_only_member_text)
        self.selection_panel.set_query_only_enabled(self._query_member_only)
        self.selection_panel.set_beam_color_mode(self._beam_color_mode)
        self.selection_panel.set_extrude(self._show_extrude)
        self.selection_panel.set_isolate_enabled(self._isolate_selection)
        self.selection_panel.set_auto_range(self._auto_contour_range)
        if self._manual_scalar_limits is not None:
            self.selection_panel.set_contour_limits(*self._manual_scalar_limits)

        if self._current_model is None:
            self.selection_panel.set_results_enabled(False)
            self.selection_panel.set_selected_component(self._current_component)
            self.selection_panel.clear_info()
            self.viewport.set_grid_visibility(self._grid_visible, rerender=False)
            self.viewport.set_background_mode(self._background_mode, rerender=False)
            self._update_model_summary()
            return

        model = self._current_model
        scalar_limits = None if self._auto_contour_range else self._manual_scalar_limits
        if self._show_extrude and not self._ensure_section_metadata_loaded():
            self._show_extrude = False
            self.selection_panel.set_extrude(False)
        self.viewport.set_extrude(self._show_extrude, rerender=False)
        self.viewport.render_model(
            model,
            result_field=self._current_result_field,
            display_component=self._current_component,
            deformation_scale=self._deformation_scale,
            color_map=self._color_map,
            scalar_bar_orientation=self._legend_orientation,
            scalar_limits_override=scalar_limits,
            overlay_text=self._build_view_overlay_text(),
            reset_camera=False,
        )
        if hasattr(self, "_empty_viewport_label"):
            self._empty_viewport_label.hide()
        self.viewport.set_grid_visibility(self._grid_visible, rerender=False)
        self.viewport.set_background_mode(self._background_mode, rerender=False)
        self.viewport.set_beam_color_mode(self._beam_color_mode, rerender=False)
        self.viewport.set_isolate_selected_beam(self._isolate_selection, rerender=False)

        self.selection_panel.set_model_up_axis(model.up_axis)
        self.selection_panel.up_axis_combo.setCurrentText(self._saved_up_axis_text)
        self.selection_panel.set_load_cases(model.load_cases)
        self.selection_panel.set_selected_load_case(self._current_load_case_id)
        self.selection_panel.set_selected_component(self._current_component)
        self.selection_panel.set_displacement_unit(self._current_result_metadata.displacement_unit)
        self.selection_panel.set_results_enabled(bool(model.load_cases))
        self.selection_panel.set_results_state(
            f"{len(model.load_cases)} cases | supports={model.support_count} | metadata={'yes' if self._current_result_metadata.results_available else 'no'}"
        )
        self.selection_panel.set_intensity_node_text(self._intensity_node_text)
        self.selection_panel.set_intensity_elevation_auto(self._intensity_elevation_auto)
        self.selection_panel.set_intensity_elevation(self._intensity_elevation)
        self.selection_panel.set_intensity_tolerance(self._intensity_tolerance)
        self.selection_panel.set_intensity_round_mm(self._intensity_round_mm)
        self.selection_panel.set_intensity_boundary_offset(self._intensity_boundary_offset)
        self.selection_panel.set_intensity_source_mode(self._intensity_source_mode)
        self.selection_panel.set_intensity_zone_mode(self._intensity_zone_mode)
        self.selection_panel.set_intensity_state(
            f"Horizontal plane follows model {model.up_axis}-up axis. Pressure uses vertical support reaction."
        )
        self._update_model_summary()

    def _create_status_bar(self) -> None:
        status_bar = QStatusBar(self)
        status_bar.showMessage("Ready. Connect to STAAD to load geometry.")
        self.setStatusBar(status_bar)

    def _update_model_summary(self) -> None:
        if not self._model_metric_labels:
            return

        if self._current_model is None:
            values = {
                "model": "No model",
                "nodes": "-",
                "beams": "-",
                "cases": "-",
                "unit": "-",
                "selection": "-",
            }
        else:
            model = self._current_model
            values = {
                "model": model.model_path.stem,
                "nodes": f"{model.node_count:,}",
                "beams": f"{model.beam_count:,}",
                "cases": f"{len(model.load_cases):,}",
                "unit": model.unit_length or "Unknown",
                "selection": self._model_metric_labels.get("selection", QLabel()).text() or "-",
            }
        for key, value in values.items():
            label = self._model_metric_labels.get(key)
            if label is not None:
                label.setText(value)

        if hasattr(self, "_mode_label"):
            component = self._current_component.upper()
            case = "-" if self._current_load_case_id is None else str(self._current_load_case_id)
            self._mode_label.setText(f"{component} / CASE {case}")

    def _connect_and_load_geometry(self) -> None:
        self.statusBar().showMessage("Connecting to STAAD and reading geometry...")
        try:
            model = self._connector.load_geometry()
        except StaadConnectionError as exc:
            self._show_error("STAAD Connection Error", str(exc))
            self.statusBar().showMessage("Failed to connect to STAAD.", 5000)
            return
        except Exception as exc:  # pragma: no cover - live IO/COM path
            self._show_error("Geometry Load Error", str(exc))
            self.statusBar().showMessage("Failed to read geometry from STAAD.", 5000)
            return

        self._result_cache.clear()
        self._current_model = model
        self._current_result_metadata = self._connector.get_result_metadata()
        self._current_result_field = None
        self._current_component = "Wireframe"
        self._current_load_case_id = model.load_cases[0].case_id if model.load_cases else None
        self._deformation_scale = 1.0
        self._auto_contour_range = True
        self._manual_scalar_limits = None
        if self._show_extrude and not self._ensure_section_metadata_loaded():
            self._show_extrude = False

        self.viewport.set_extrude(self._show_extrude, rerender=False)
        self.viewport.render_model(
            model,
            display_component=self._current_component,
            deformation_scale=self._deformation_scale,
            color_map=self._color_map,
            scalar_bar_orientation=self._legend_orientation,
            overlay_text=self._build_view_overlay_text(),
        )
        if hasattr(self, "_empty_viewport_label"):
            self._empty_viewport_label.hide()
        self.viewport.set_beam_color_mode(self._beam_color_mode, rerender=False)
        self.viewport.set_background_mode(self._background_mode, rerender=False)
        self.viewport.set_isolate_selected_beam(self._isolate_selection, rerender=False)
        self.selection_panel.set_model_up_axis(model.up_axis)
        self.selection_panel.up_axis_combo.setCurrentText(self._saved_up_axis_text)
        self.selection_panel.set_load_cases(model.load_cases)
        self.selection_panel.set_selected_component(self._current_component)
        self.selection_panel.scale_spin.setValue(self._deformation_scale)
        target_case_id = self._saved_load_case_id if self._saved_load_case_id is not None else self._current_load_case_id
        self.selection_panel.set_selected_load_case(target_case_id)
        self._current_load_case_id = self.selection_panel.selected_load_case_id()
        self.selection_panel.set_displacement_unit(self._current_result_metadata.displacement_unit)
        results_enabled = bool(model.load_cases)
        self.selection_panel.set_results_enabled(results_enabled)
        self.selection_panel.set_results_state(
            f"{len(model.load_cases)} cases | supports={model.support_count} | metadata={'yes' if self._current_result_metadata.results_available else 'no'}"
        )
        self.selection_panel.set_intensity_node_text(self._intensity_node_text)
        self.selection_panel.set_intensity_elevation_auto(self._intensity_elevation_auto)
        self.selection_panel.set_intensity_elevation(self._intensity_elevation)
        self.selection_panel.set_intensity_tolerance(self._intensity_tolerance)
        self.selection_panel.set_intensity_round_mm(self._intensity_round_mm)
        self.selection_panel.set_intensity_boundary_offset(self._intensity_boundary_offset)
        self.selection_panel.set_intensity_source_mode(self._intensity_source_mode)
        self.selection_panel.set_intensity_zone_mode(self._intensity_zone_mode)
        self.selection_panel.set_intensity_state(
            f"Horizontal plane follows model {model.up_axis}-up axis. Pressure uses vertical support reaction."
        )
        self.selection_panel.set_color_map(self._color_map)
        self.selection_panel.set_legend_orientation(self._legend_orientation)
        self.selection_panel.set_scalar_bar_style(self._scalar_bar_style)
        self.selection_panel.set_reference_mode(self._reference_mode)
        self.selection_panel.set_non_display_member_text(self._non_display_member_text)
        self.selection_panel.set_display_only_member_text(self._display_only_member_text)
        self.selection_panel.set_query_only_enabled(self._query_member_only)
        self.selection_panel.set_beam_color_mode(self._beam_color_mode)
        self.selection_panel.set_background_mode(self._background_mode)
        self.viewport.set_scalar_bar_style(self._scalar_bar_style, rerender=False)
        self.selection_panel.set_auto_range(self._auto_contour_range)
        if self._manual_scalar_limits is not None:
            self.selection_panel.set_contour_limits(*self._manual_scalar_limits)
        self.selection_panel.set_extrude(self._show_extrude)
        self.selection_panel.set_isolate_enabled(self._isolate_selection)
        self.selection_panel.clear_info()
        selection_label = self._model_metric_labels.get("selection")
        if selection_label is not None:
            selection_label.setText("-")
        self._change_up_axis(self.selection_panel.up_axis_combo.currentText())
        self._update_model_summary()

        if results_enabled and self._current_load_case_id is not None:
            if self._current_result_metadata.results_available:
                self.statusBar().showMessage(
                    f"{model.model_path.name} | Unit: {model.unit_length or 'Unknown'} | Nodes: {model.node_count} | Beams: {model.beam_count} | Results ready. Configure displacement or pressure intensity then click Apply."
                )
            else:
                self.statusBar().showMessage(
                    f"{model.model_path.name} | Unit: {model.unit_length or 'Unknown'} | Nodes: {model.node_count} | Beams: {model.beam_count} | Load cases found, but result metadata was not confirmed. You can still try Apply."
                )
        else:
            self.statusBar().showMessage(
                f"{model.model_path.name} | Unit: {model.unit_length or 'Unknown'} | Nodes: {model.node_count} | Beams: {model.beam_count} | Click a beam or joint to inspect it."
            )

    def _reload_geometry(self) -> None:
        self._connect_and_load_geometry()

    def _set_joint_visibility(self, visible: bool) -> None:
        self.viewport.set_joint_visibility(visible)
        self.statusBar().showMessage("Joints visible." if visible else "Joints hidden.", 3000)

    def _toggle_joint_visibility(self) -> None:
        self._set_joint_visibility(not self.viewport.are_joints_visible())

    def _set_grid_visibility(self, visible: bool) -> None:
        self._grid_visible = visible
        self.viewport.set_grid_visibility(visible)
        self.statusBar().showMessage("Grid visible." if visible else "Grid hidden.", 3000)
        self._save_persisted_settings()
        self._update_model_summary()

    def _set_extrude_mode(self, enabled: bool) -> None:
        self._show_extrude = enabled
        if enabled and not self._ensure_section_metadata_loaded():
            self._show_extrude = False
            self.selection_panel.set_extrude(False)
            self.viewport.set_extrude(False, rerender=True)
            self._save_persisted_settings()
            return
        self.viewport.set_extrude(enabled, rerender=True)
        self.statusBar().showMessage("Extrude mode enabled." if enabled else "Wireframe mode enabled.", 3000)
        self._save_persisted_settings()

    def _ensure_section_metadata_loaded(self) -> bool:
        if self._current_model is None or self._current_model.section_metadata_loaded:
            return True

        self.statusBar().showMessage("Reading beam section metadata for extrude...")
        try:
            self._connector.load_section_metadata(self._current_model)
        except StaadConnectionError as exc:
            self._show_error("STAAD Connection Error", str(exc))
            self.statusBar().showMessage("Failed to read beam section metadata.", 5000)
            return False
        except Exception as exc:  # pragma: no cover - live COM path
            self._show_error("Section Metadata Error", str(exc))
            self.statusBar().showMessage("Failed to read beam section metadata.", 5000)
            return False

        self.statusBar().showMessage("Beam section metadata loaded for extrude.", 3000)
        return True

    def _set_beam_color_mode(self, mode: str) -> None:
        self._beam_color_mode = mode
        self.viewport.set_beam_color_mode(mode, rerender=True)
        self.statusBar().showMessage(f"Beam colors: {mode}.", 3000)
        self._save_persisted_settings()

    def _set_background_mode(self, mode: str) -> None:
        self._background_mode = "White" if str(mode).lower().startswith("w") else "Dark"
        self.viewport.set_background_mode(self._background_mode, rerender=True)
        if hasattr(self, "_empty_viewport_label"):
            self._empty_viewport_label.setVisible(self._current_model is None)
        self.statusBar().showMessage(f"Viewport background: {self._background_mode}.", 3000)
        self._save_persisted_settings()

    def _set_isolate_selected_beam(self, enabled: bool) -> None:
        self._isolate_selection = enabled
        self.viewport.set_isolate_selected_beam(enabled, rerender=True)
        if enabled:
            self.statusBar().showMessage("Isolate selected beam is on.", 3000)
        else:
            self.statusBar().showMessage("Isolate selected beam is off.", 3000)
        self._save_persisted_settings()

    def _change_up_axis(self, value: str) -> None:
        if self._current_model is None:
            return

        if value == "Auto":
            up_axis = self._current_model.up_axis
        else:
            up_axis = value[0]

        self.viewport.apply_up_axis(up_axis)
        self.selection_panel.set_model_up_axis(up_axis)
        self.statusBar().showMessage(f"Viewport set to {up_axis}-up.", 3000)
        self._save_persisted_settings()

    def _apply_selected_view(self) -> None:
        self._current_load_case_id = self.selection_panel.selected_load_case_id()
        self._current_component = self.selection_panel.selected_component()
        self._deformation_scale = 1.0
        self._color_map = self.selection_panel.selected_color_map()
        self._legend_orientation = self.selection_panel.selected_legend_orientation()
        self._scalar_bar_style = self.selection_panel.selected_scalar_bar_style()
        self._reference_mode = self.selection_panel.selected_reference_mode()
        self._non_display_member_text = self.selection_panel.selected_non_display_member_text()
        try:
            self._non_display_member_ids = set(self._parse_member_group(self._non_display_member_text))
        except ValueError as exc:
            self._show_error("Non-Display Member List Error", str(exc))
            return
        if self._current_model is not None and self._non_display_member_ids:
            model_beam_ids = {beam.beam_id for beam in self._current_model.beams}
            missing_members = sorted(self._non_display_member_ids - model_beam_ids)
            if missing_members:
                self.statusBar().showMessage(
                    f"Non-Display member(s) not found and ignored: {', '.join(str(item) for item in missing_members[:20])}",
                    5000,
                )
                self._non_display_member_ids &= model_beam_ids
        self._display_only_member_text = self.selection_panel.selected_display_only_member_text()
        try:
            self._display_only_member_ids = set(self._parse_member_group(self._display_only_member_text))
        except ValueError as exc:
            self._show_error("Display Only Member List Error", str(exc))
            return
        if self._current_model is not None and self._display_only_member_ids:
            model_beam_ids = {beam.beam_id for beam in self._current_model.beams}
            missing_members = sorted(self._display_only_member_ids - model_beam_ids)
            if missing_members:
                self.statusBar().showMessage(
                    f"Display Only member(s) not found and ignored: {', '.join(str(item) for item in missing_members[:20])}",
                    5000,
                )
                self._display_only_member_ids &= model_beam_ids
        self._query_member_only = self.selection_panel.is_query_only_enabled()
        self._intensity_node_text = self.selection_panel.selected_intensity_node_text()
        self._intensity_elevation_auto = self.selection_panel.is_intensity_elevation_auto()
        self._intensity_elevation = self.selection_panel.selected_intensity_elevation()
        self._intensity_tolerance = self.selection_panel.selected_intensity_tolerance()
        self._intensity_round_mm = self.selection_panel.selected_intensity_round_mm()
        self._intensity_boundary_offset = self.selection_panel.selected_intensity_boundary_offset()
        self._intensity_source_mode = self.selection_panel.selected_intensity_source_mode()
        self._intensity_zone_mode = self.selection_panel.selected_intensity_zone_mode()
        self._auto_contour_range = self.selection_panel.is_auto_range()
        self._manual_scalar_limits = None if self._auto_contour_range else self.selection_panel.contour_limits()

        if not self._auto_contour_range and self._manual_scalar_limits is None:
            self._show_error("Contour Range Error", "Contour Min must be smaller than Contour Max.")
            return

        self.viewport.set_scalar_bar_style(self._scalar_bar_style, rerender=False)
        self._apply_result_view()
        self._save_persisted_settings()

    def _apply_result_view(self, reload_results: bool = True) -> None:
        if self._current_model is None:
            return

        scalar_limits = None if self._auto_contour_range else self._manual_scalar_limits

        if self._current_component == "Wireframe" or self._current_load_case_id is None:
            self._current_result_field = None
            self.viewport.render_model(
                self._current_model,
                display_component=self._current_component,
                deformation_scale=self._deformation_scale,
                color_map=self._color_map,
                scalar_bar_orientation=self._legend_orientation,
                scalar_limits_override=scalar_limits,
                overlay_text=self._build_view_overlay_text(),
                reset_camera=False,
            )
            self.statusBar().showMessage("Wireframe view active.", 3000)
            self._update_model_summary()
            return

        if self._current_component == "PRESSURE_INTENSITY":
            self._apply_pressure_intensity_view(scalar_limits, reload_results=reload_results)
            return

        query_member_ids = self._query_member_ids_for_result()
        key = self._displacement_result_cache_key(query_member_ids)
        if reload_results and key not in self._result_cache:
            self.statusBar().showMessage(
                f"Reading {self._current_component} results for load case {self._current_load_case_id}...",
            )
            try:
                self._result_cache[key] = self._connector.load_global_displacement_field(
                    self._current_model,
                    self._current_load_case_id,
                    self._current_component,
                    include_beam_values=True,
                    beam_ids=query_member_ids,
                )
            except Exception as exc:
                self._show_error("Result Import Error", str(exc))
                self.selection_panel.set_selected_component("Wireframe")
                self._current_component = "Wireframe"
                self._current_result_field = None
                self.viewport.render_model(
                    self._current_model,
                    display_component="Wireframe",
                    color_map=self._color_map,
                    scalar_bar_orientation=self._legend_orientation,
                    overlay_text=self._build_view_overlay_text(),
                    reset_camera=False,
                )
                self.statusBar().showMessage("Failed to read analysis results.", 5000)
                return

        base_field = self._result_cache.get(key)
        self._current_result_field = self._build_display_field(base_field)
        if self._current_result_field is not None and self._auto_contour_range:
            self.selection_panel.set_contour_limits(
                self._current_result_field.min_value,
                self._current_result_field.max_value,
            )

        self.viewport.render_model(
            self._current_model,
            result_field=self._current_result_field,
            display_component=self._current_component,
            deformation_scale=self._deformation_scale,
            color_map=self._color_map,
            scalar_bar_orientation=self._legend_orientation,
            scalar_limits_override=scalar_limits,
            overlay_text=self._build_view_overlay_text(),
            reset_camera=False,
        )
        unit = self._current_result_field.unit if self._current_result_field is not None else None
        suffix = f" ({unit})" if unit else ""
        excluded_count = (
            len(self._current_result_field.excluded_beam_ids)
            if isinstance(self._current_result_field, ModelResultField)
            else 0
        )
        display_only_count = (
            len(self._current_result_field.beam_values)
            if isinstance(self._current_result_field, ModelResultField) and self._display_only_member_ids
            else 0
        )
        state_suffix = ""
        if display_only_count:
            state_suffix += f" | display-only={display_only_count}"
        if excluded_count:
            state_suffix += f" | non-display={excluded_count}"
        self.selection_panel.set_results_state(
            f"{len(self._current_model.load_cases)} cases | supports={self._current_model.support_count}"
            f" | metadata={'yes' if self._current_result_metadata.results_available else 'no'}{state_suffix}"
        )
        self.statusBar().showMessage(
            f"Showing {self._current_component}{suffix} for load case {self._current_load_case_id} ({self._reference_mode}).",
            4000,
        )
        self._update_model_summary()

    def _apply_pressure_intensity_view(
        self,
        scalar_limits: tuple[float, float] | None,
        reload_results: bool = True,
    ) -> None:
        if self._current_model is None or self._current_load_case_id is None:
            return

        try:
            node_ids = self._parse_node_group(self._intensity_node_text)
            if not node_ids:
                raise ValueError("Enter at least one support node for Pressure Intensity.")
            missing = [node_id for node_id in node_ids if node_id not in self._current_model.nodes]
            if missing:
                raise ValueError(f"Node(s) not found in model: {', '.join(str(item) for item in missing[:20])}")

            up_axis = self._current_model.up_axis.upper()
            elevation = self._resolve_intensity_elevation(node_ids, up_axis)
            off_plane = self._off_plane_nodes(node_ids, up_axis, elevation, self._intensity_tolerance)
            if off_plane:
                preview = ", ".join(f"{node_id} ({delta:+.4f})" for node_id, delta in off_plane[:12])
                raise ValueError(
                    f"Node group is not on one horizontal plane. Off-plane node delta(s): {preview}."
                )

            component = "FY" if up_axis == "Y" else "FZ"
            key = (
                self._current_load_case_id,
                self._current_component,
                tuple(node_ids),
                up_axis,
                round(elevation, 8),
                round(self._intensity_tolerance, 4),
                round(self._intensity_round_mm, 4),
                round(self._intensity_boundary_offset, 8),
                self._intensity_source_mode,
                self._effective_intensity_zone_mode(),
            )
            if reload_results and key not in self._result_cache:
                source_text = "support reaction force" if self._is_support_reaction_force_mode() else "support reaction pressure"
                self.statusBar().showMessage(
                    f"Reading {component} {source_text} for {len(node_ids)} node(s)..."
                )
                reactions, force_unit = self._connector.load_support_reaction_values(
                    self._current_load_case_id,
                    node_ids,
                    component,
                )
                self._result_cache[key] = self._build_pressure_intensity_field(
                    node_ids,
                    reactions,
                    component,
                    force_unit,
                    up_axis,
                    elevation,
                )
        except Exception as exc:
            self._show_error("Pressure Intensity Error", str(exc))
            self.selection_panel.set_intensity_state(str(exc))
            self.selection_panel.set_selected_component("Wireframe")
            self._current_component = "Wireframe"
            self._current_result_field = None
            self.viewport.render_model(
                self._current_model,
                display_component="Wireframe",
                color_map=self._color_map,
                scalar_bar_orientation=self._legend_orientation,
                overlay_text=self._build_view_overlay_text(),
                reset_camera=False,
            )
            self.statusBar().showMessage("Failed to build pressure intensity view.", 5000)
            return

        field = self._result_cache.get(key)
        self._current_result_field = field if isinstance(field, HorizontalPressureIntensityField) else None
        if self._current_result_field is not None and self._auto_contour_range:
            self.selection_panel.set_contour_limits(
                self._current_result_field.min_value,
                self._current_result_field.max_value,
            )

        self.viewport.render_model(
            self._current_model,
            result_field=self._current_result_field,
            display_component=self._current_component,
            deformation_scale=1.0,
            color_map=self._color_map,
            scalar_bar_orientation=self._legend_orientation,
            scalar_limits_override=scalar_limits,
            overlay_text=self._build_view_overlay_text(),
            reset_camera=False,
        )
        if self._current_result_field is not None:
            node_count = len(self._current_result_field.node_values)
            warning = self._current_result_field.warning
            state = (
                f"{node_count} nodes | {self._intensity_source_mode.lower()}"
                f" | elevation {self._current_result_field.elevation:.4f}"
            )
            if warning:
                state = f"{state} | {warning}"
            self.selection_panel.set_intensity_state(state)
            view_name = "support reaction zones" if self._is_support_reaction_force_mode() else "horizontal pressure intensity"
            self.statusBar().showMessage(
                f"Showing {view_name} for load case {self._current_load_case_id}.",
                4000,
            )
        self._update_model_summary()

    def _build_display_field(self, base_field: ModelResultField | None) -> ModelResultField | None:
        if base_field is None:
            return None
        display_field = base_field
        if self._reference_mode != "Relative to Support":
            return self._apply_member_display_filters(display_field)
        if self._current_model is None:
            return self._apply_member_display_filters(display_field)

        support_node_ids = self._current_model.support_node_ids_for_component(base_field.component)
        if not support_node_ids:
            self.statusBar().showMessage(
                f"No FIXED/PINNED support nodes were found for {base_field.component}. Falling back to absolute values.",
                5000,
            )
            return self._apply_member_display_filters(display_field)

        support_values = [
            base_field.node_values[node_id]
            for node_id in support_node_ids
            if node_id in base_field.node_values
        ]
        if not support_values:
            self.statusBar().showMessage(
                f"Support nodes were found, but no support displacement values were available for {base_field.component}.",
                5000,
            )
            return self._apply_member_display_filters(display_field)

        reference_value = sum(support_values) / len(support_values)
        display_field = replace(
            base_field,
            node_values={node_id: value - reference_value for node_id, value in base_field.node_values.items()},
            beam_values={beam_id: value - reference_value for beam_id, value in base_field.beam_values.items()},
            beam_peak_positions=dict(base_field.beam_peak_positions),
        )
        return self._apply_member_display_filters(display_field)

    def _apply_member_display_filters(self, field: ModelResultField) -> ModelResultField:
        if not self._display_only_member_ids and not self._non_display_member_ids:
            return field

        source_beam_ids = set(field.beam_values)
        if not source_beam_ids:
            return field

        if self._display_only_member_ids:
            kept = source_beam_ids & self._display_only_member_ids
            excluded = source_beam_ids - kept
        else:
            kept = set(source_beam_ids)
            excluded = set()

        non_display = kept & self._non_display_member_ids
        kept -= non_display
        excluded |= non_display

        if kept == source_beam_ids and not excluded:
            return field

        node_values = dict(field.node_values)
        if self._display_only_member_ids and self._current_model is not None:
            kept_node_ids = set()
            for beam in self._current_model.beams:
                if beam.beam_id in kept:
                    kept_node_ids.add(beam.start_node_id)
                    kept_node_ids.add(beam.end_node_id)
            node_values = {node_id: value for node_id, value in field.node_values.items() if node_id in kept_node_ids}

        return replace(
            field,
            node_values=node_values,
            beam_values={beam_id: value for beam_id, value in field.beam_values.items() if beam_id in kept},
            beam_peak_positions={
                beam_id: value for beam_id, value in field.beam_peak_positions.items() if beam_id in kept
            },
            excluded_beam_ids=excluded,
        )

    def _query_member_ids_for_result(self) -> set[int] | None:
        if not self._query_member_only or not self._display_only_member_ids:
            return None
        if self._current_model is None:
            return set(self._display_only_member_ids)
        model_beam_ids = {beam.beam_id for beam in self._current_model.beams}
        return set(self._display_only_member_ids) & model_beam_ids

    def _displacement_result_cache_key(self, query_member_ids: set[int] | None) -> tuple[object, ...]:
        if query_member_ids:
            return (
                self._current_load_case_id,
                self._current_component,
                "query",
                tuple(sorted(query_member_ids)),
            )
        return (self._current_load_case_id, self._current_component, "all")

    def _parse_node_group(self, text: str) -> list[int]:
        return self._parse_id_group(text, "node")

    def _parse_member_group(self, text: str) -> list[int]:
        return self._parse_id_group(text, "member")

    def _parse_id_group(self, text: str, item_name: str) -> list[int]:
        normalized = text.replace(",", " ").replace(";", " ")
        tokens = [token for token in normalized.split() if token]
        values: list[int] = []
        index = 0
        while index < len(tokens):
            token = tokens[index].upper()
            if token == "TO":
                index += 1
                continue
            if not token.lstrip("+-").isdigit():
                raise ValueError(f"Invalid {item_name} token: {tokens[index]}")

            start_value = int(token)
            if index + 2 < len(tokens) and tokens[index + 1].upper() == "TO":
                end_token = tokens[index + 2]
                if not end_token.lstrip("+-").isdigit():
                    raise ValueError(f"Invalid TO {item_name} token: {end_token}")
                end_value = int(end_token)
                step = 1 if end_value >= start_value else -1
                values.extend(range(start_value, end_value + step, step))
                index += 3
            else:
                values.append(start_value)
                index += 1

        seen: set[int] = set()
        deduped: list[int] = []
        for value in values:
            if value not in seen:
                seen.add(value)
                deduped.append(value)
        return deduped

    def _resolve_intensity_elevation(self, node_ids: list[int], up_axis: str) -> float:
        if not self._intensity_elevation_auto:
            return self._intensity_elevation

        values: list[tuple[int, float]] = []
        for node_id in node_ids:
            node = self._current_model.nodes[node_id]
            values.append((node_id, node.y if up_axis == "Y" else node.z))

        elevation = sum(value for _, value in values) / len(values)
        self._intensity_elevation = elevation
        self.selection_panel.set_intensity_elevation(elevation)
        return elevation

    def _off_plane_nodes(
        self,
        node_ids: list[int],
        up_axis: str,
        elevation: float,
        tolerance_percent: float,
    ) -> list[tuple[int, float]]:
        off_plane: list[tuple[int, float]] = []
        plane_tolerance = abs(elevation) * max(tolerance_percent, 0.0) / 100.0
        for node_id in node_ids:
            node = self._current_model.nodes[node_id]
            value = node.y if up_axis == "Y" else node.z
            delta = value - elevation
            if abs(delta) > plane_tolerance:
                off_plane.append((node_id, delta))
        return off_plane

    def _model_length_to_mm_factor(self, unit: str | None) -> float:
        if unit is None:
            return 1.0
        normalized = unit.strip().upper()
        return {
            "MM": 1.0,
            "MMS": 1.0,
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
        }.get(normalized, 1.0)

    def _build_pressure_intensity_field(
        self,
        node_ids: list[int],
        reactions: dict[int, float],
        component: str,
        force_unit: str | None,
        up_axis: str,
        elevation: float,
    ) -> HorizontalPressureIntensityField:
        axis_a, axis_b = self._horizontal_axis_values(node_ids, up_axis)
        a_min, a_max = min(axis_a), max(axis_a)
        b_min, b_max = min(axis_b), max(axis_b)
        span_a = max(a_max - a_min, 1e-9)
        span_b = max(b_max - b_min, 1e-9)
        fallback_offset = max(span_a, span_b, 1.0) * 0.15
        min_visual_offset = max(span_a, span_b, 1.0) * 0.05
        offset = max(
            self._intensity_boundary_offset if self._intensity_boundary_offset > 0.0 else fallback_offset,
            min_visual_offset,
        )
        zone_radius = offset

        boundary = (
            a_min - offset,
            a_max + offset,
            b_min - offset,
            b_max + offset,
        )
        tributary_areas: dict[int, float] = {}
        warning = None
        pressure_values: dict[int, float] = {}
        if self._is_support_reaction_force_mode():
            pressure_values = {node_id: reactions.get(node_id, 0.0) for node_id in node_ids}
            unit = force_unit or "force"
            source_label = "Support Reaction"
            zone_mode = "Column Zones"
        else:
            tributary_areas, warning = self._estimate_tributary_areas(axis_a, axis_b, node_ids, boundary)
            for node_id in node_ids:
                area = tributary_areas.get(node_id, 0.0)
                if area <= 0.0:
                    pressure_values[node_id] = 0.0
                else:
                    pressure_values[node_id] = reactions.get(node_id, 0.0) / area

            length_unit = self._current_model.unit_length or "model"
            unit = f"{force_unit or 'force'}/{length_unit}^2"
            source_label = "Support"
            zone_mode = self._intensity_zone_mode
        return HorizontalPressureIntensityField(
            case_id=self._current_load_case_id,
            component=component,
            unit=unit,
            up_axis=up_axis,
            elevation=elevation,
            node_values=pressure_values,
            reaction_values=dict(reactions),
            tributary_areas=tributary_areas,
            boundary=boundary,
            zone_mode=zone_mode,
            zone_radius=zone_radius,
            warning=warning,
        )

    def _horizontal_axis_values(self, node_ids: list[int], up_axis: str) -> tuple[list[float], list[float]]:
        axis_a: list[float] = []
        axis_b: list[float] = []
        for node_id in node_ids:
            node = self._current_model.nodes[node_id]
            axis_a.append(node.x)
            axis_b.append(node.z if up_axis == "Y" else node.y)
        return axis_a, axis_b

    def _is_support_reaction_force_mode(self) -> bool:
        return self._intensity_source_mode == "Support Reaction Force"

    def _effective_intensity_zone_mode(self) -> str:
        return "Column Zones" if self._is_support_reaction_force_mode() else self._intensity_zone_mode

    def _estimate_tributary_areas(
        self,
        axis_a: list[float],
        axis_b: list[float],
        node_ids: list[int],
        boundary: tuple[float, float, float, float],
    ) -> tuple[dict[int, float], str | None]:
        a_min, a_max, b_min, b_max = boundary
        grid_count = 120
        da = (a_max - a_min) / grid_count
        db = (b_max - b_min) / grid_count
        cell_area = da * db
        counts = {node_id: 0 for node_id in node_ids}
        support_points = list(zip(axis_a, axis_b, node_ids))

        for ia in range(grid_count):
            a = a_min + (ia + 0.5) * da
            for ib in range(grid_count):
                b = b_min + (ib + 0.5) * db
                nearest_id = min(
                    support_points,
                    key=lambda item: (item[0] - a) ** 2 + (item[1] - b) ** 2,
                )[2]
                counts[nearest_id] += 1

        areas = {node_id: counts[node_id] * cell_area for node_id in node_ids}
        warning = None
        if len(support_points) > 1:
            min_distance = min(
                ((a1 - a2) ** 2 + (b1 - b2) ** 2) ** 0.5
                for index, (a1, b1, _) in enumerate(support_points)
                for a2, b2, _ in support_points[index + 1 :]
            )
            grid_size = max(abs(da), abs(db))
            if min_distance < grid_size * 3.0:
                warning = "Some support nodes are closer than the intensity grid size."
        return areas, warning

    def _handle_beam_selected(self, beam: BeamData | None) -> None:
        if beam is None:
            self.selection_panel.set_beam_info(None, None, None)
            self.selection_panel.set_beam_section(None)
            self.selection_panel.set_beam_result(None, None, None, None)
            selection_label = self._model_metric_labels.get("selection")
            if selection_label is not None:
                selection_label.setText("-")
            return
        self.selection_panel.set_beam_info(beam.beam_id, beam.start_node_id, beam.end_node_id)
        self.selection_panel.set_beam_section(beam)
        self.selection_panel.set_joint_info(None, None)
        selection_label = self._model_metric_labels.get("selection")
        if selection_label is not None:
            selection_label.setText(f"Beam {beam.beam_id}")
        display_field = self._current_result_field
        base_field = self._base_result_field()
        if display_field is not None and base_field is not None and base_field.beam_values:
            display_value = display_field.beam_values.get(beam.beam_id)
            global_value = base_field.beam_values.get(beam.beam_id)
            position = base_field.beam_peak_positions.get(beam.beam_id)
            unit = base_field.unit or display_field.unit
            self.selection_panel.set_beam_result(display_value, global_value, position, unit)
        else:
            self.selection_panel.set_beam_result(None, None, None, None)
        self.statusBar().showMessage(
            f"Selected beam {beam.beam_id} ({beam.start_node_id} -> {beam.end_node_id}).",
            4000,
        )

    def _handle_joint_selected(self, node: NodeData | None) -> None:
        if node is None:
            self.selection_panel.set_joint_info(None, None)
            selection_label = self._model_metric_labels.get("selection")
            if selection_label is not None:
                selection_label.setText("-")
            return
        self.selection_panel.set_beam_info(None, None, None)
        self.selection_panel.set_beam_section(None)
        self.selection_panel.set_beam_result(None, None, None, None)
        self.selection_panel.set_joint_info(node.node_id, (node.x, node.y, node.z))
        selection_label = self._model_metric_labels.get("selection")
        if selection_label is not None:
            selection_label.setText(f"Joint {node.node_id}")
        self.statusBar().showMessage(f"Selected joint {node.node_id}.", 4000)

    def _show_error(self, title: str, message: str) -> None:
        QMessageBox.critical(self, title, message)

    def closeEvent(self, event) -> None:
        self._save_persisted_settings()
        super().closeEvent(event)

    def _load_persisted_settings(self) -> None:
        self._grid_visible = self._settings.value("view/grid_visible", True, bool)
        # Keep startup geometry loading fast. Extrude requires per-beam section
        # metadata from OpenSTAAD, so it must be an explicit per-session choice.
        self._show_extrude = False
        self._beam_color_mode = self._settings.value("view/beam_color_mode", "Default", str)
        self._background_mode = self._settings.value("view/background_mode", "Dark", str)
        self._background_mode = "White" if str(self._background_mode).lower().startswith("w") else "Dark"
        self._isolate_selection = self._settings.value("view/isolate_selected_beam", False, bool)
        self._metric_cards_on_left = self._settings.value("view/metric_cards_on_left", False, bool)
        self._current_component = self._settings.value("results/display_component", "Wireframe", str)
        self._deformation_scale = 1.0
        self._color_map = self._settings.value("contour/color_map", "jet", str)
        self._legend_orientation = self._settings.value("contour/legend_orientation", "Vertical", str)
        if str(self._legend_orientation).lower().startswith("h"):
            self._legend_orientation = "Horizontal"
        else:
            self._legend_orientation = "Vertical"
        self._scalar_bar_style = self._settings.value("contour/scalar_bar_style", "App", str)
        if str(self._scalar_bar_style).strip().lower().startswith("pyvista"):
            self._scalar_bar_style = "PyVista Default"
        else:
            self._scalar_bar_style = "App"
        self._reference_mode = self._settings.value("contour/reference_mode", "Global Absolute", str)
        if self._reference_mode == "Absolute":
            self._reference_mode = "Global Absolute"
        self._non_display_member_text = self._settings.value("results/non_display_members", "", str)
        try:
            self._non_display_member_ids = set(self._parse_member_group(self._non_display_member_text))
        except ValueError:
            self._non_display_member_ids = set()
        self._display_only_member_text = self._settings.value("results/display_only_members", "", str)
        try:
            self._display_only_member_ids = set(self._parse_member_group(self._display_only_member_text))
        except ValueError:
            self._display_only_member_ids = set()
        self._query_member_only = self._settings.value("results/query_member_only", False, bool)
        self._intensity_node_text = self._settings.value("intensity/node_group", "", str)
        self._intensity_elevation_auto = self._settings.value("intensity/elevation_auto", True, bool)
        self._intensity_elevation = float(self._settings.value("intensity/elevation", 0.0))
        self._intensity_tolerance = float(self._settings.value("intensity/mean_tolerance_percent", 5.0))
        self._intensity_round_mm = float(self._settings.value("intensity/round_mm", 100.0))
        self._intensity_boundary_offset = float(self._settings.value("intensity/boundary_offset", 1.0))
        self._intensity_source_mode = self._settings.value("intensity/source_mode", "Pressure / Area", str)
        if self._intensity_source_mode not in {"Pressure / Area", "Support Reaction Force"}:
            self._intensity_source_mode = "Pressure / Area"
        self._intensity_zone_mode = self._settings.value("intensity/zone_mode", "Full Plane", str)
        if self._intensity_zone_mode != "Column Zones":
            self._intensity_zone_mode = "Full Plane"
        self._auto_contour_range = self._settings.value("contour/auto_range", True, bool)
        minimum = self._settings.value("contour/minimum", None)
        maximum = self._settings.value("contour/maximum", None)
        if minimum is not None and maximum is not None:
            self._manual_scalar_limits = (float(minimum), float(maximum))
        else:
            self._manual_scalar_limits = None
        self._saved_up_axis_text = self._settings.value("view/up_axis", "Auto", str)
        saved_case = self._settings.value("results/load_case_id", None)
        self._saved_load_case_id = None if saved_case in (None, "") else int(saved_case)
        geometry = self._settings.value("window/geometry")
        state = self._settings.value("window/state")
        self._saved_geometry = geometry
        self._saved_window_state = state

    def _apply_persisted_ui_state(self) -> None:
        if self._saved_geometry is not None:
            self.restoreGeometry(self._saved_geometry)
        if self._saved_window_state is not None:
            self.restoreState(self._saved_window_state)

    def _save_persisted_settings(self) -> None:
        self._settings.setValue("view/grid_visible", self._grid_visible)
        self._settings.setValue("view/extrude", False)
        self._settings.setValue("view/beam_color_mode", self._beam_color_mode)
        self._settings.setValue("view/background_mode", self._background_mode)
        self._settings.setValue("view/isolate_selected_beam", self._isolate_selection)
        self._settings.setValue("view/metric_cards_on_left", self._metric_cards_on_left)
        if hasattr(self, "selection_panel"):
            self._settings.setValue("view/up_axis", self.selection_panel.up_axis_combo.currentText())
            self._settings.setValue("results/display_component", self.selection_panel.selected_component())
            current_case = self.selection_panel.selected_load_case_id()
            self._settings.setValue("results/load_case_id", "" if current_case is None else current_case)
            self._settings.setValue("results/scale", 1.0)
            self._settings.setValue("results/non_display_members", self.selection_panel.selected_non_display_member_text())
            self._settings.setValue("results/display_only_members", self.selection_panel.selected_display_only_member_text())
            self._settings.setValue("results/query_member_only", self.selection_panel.is_query_only_enabled())
            self._settings.setValue("contour/auto_range", self.selection_panel.is_auto_range())
            limits = self.selection_panel.contour_limits()
            if limits is not None:
                self._settings.setValue("contour/minimum", limits[0])
                self._settings.setValue("contour/maximum", limits[1])
            self._settings.setValue("contour/color_map", self.selection_panel.selected_color_map())
            self._settings.setValue("contour/legend_orientation", self.selection_panel.selected_legend_orientation())
            self._settings.setValue("contour/scalar_bar_style", self.selection_panel.selected_scalar_bar_style())
            self._settings.setValue("contour/reference_mode", self.selection_panel.selected_reference_mode())
            self._settings.setValue("intensity/node_group", self.selection_panel.selected_intensity_node_text())
            self._settings.setValue("intensity/elevation_auto", self.selection_panel.is_intensity_elevation_auto())
            self._settings.setValue("intensity/elevation", self.selection_panel.selected_intensity_elevation())
            self._settings.setValue("intensity/mean_tolerance_percent", self.selection_panel.selected_intensity_tolerance())
            self._settings.setValue("intensity/round_mm", self.selection_panel.selected_intensity_round_mm())
            self._settings.setValue(
                "intensity/boundary_offset",
                self.selection_panel.selected_intensity_boundary_offset(),
            )
            self._settings.setValue("intensity/source_mode", self.selection_panel.selected_intensity_source_mode())
            self._settings.setValue("intensity/zone_mode", self.selection_panel.selected_intensity_zone_mode())
            self._settings.setValue("view/beam_color_mode", self.selection_panel.selected_beam_color_mode())
            self._settings.setValue("view/background_mode", self.selection_panel.selected_background_mode())
            self._settings.setValue("view/isolate_selected_beam", self.selection_panel.is_isolate_enabled())
            self._settings.setValue("view/metric_cards_on_left", self.selection_panel.are_metrics_on_left_enabled())
        self._settings.setValue("window/geometry", self.saveGeometry())
        self._settings.setValue("window/state", self.saveState())

    def _base_result_field(self) -> ModelResultField | None:
        if self._current_load_case_id is None:
            return None
        key = self._displacement_result_cache_key(self._query_member_ids_for_result())
        field = self._result_cache.get(key)
        return field if isinstance(field, ModelResultField) else None

    def _build_view_overlay_text(self) -> str | None:
        if (
            self._current_model is None
            or self._current_component == "Wireframe"
            or self._current_load_case_id is None
        ):
            return None

        case_label = str(self._current_load_case_id)
        for load_case in self._current_model.load_cases:
            if load_case.case_id == self._current_load_case_id:
                case_label = load_case.display_name
                break

        if self._current_component == "PRESSURE_INTENSITY" and self._is_support_reaction_force_mode():
            display_label = "Support Reaction Force"
        else:
            display_label = {
                "DX": "DX (Global X)",
                "DY": "DY (Global Y)",
                "DZ": "DZ (Global Z)",
                "PRESSURE_INTENSITY": "Horizontal Pressure Intensity",
            }.get(self._current_component, self._current_component)

        return f"Case: {case_label}\nDisplay: {display_label}"
