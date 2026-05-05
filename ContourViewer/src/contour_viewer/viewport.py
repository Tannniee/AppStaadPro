from __future__ import annotations

import math
import numpy as np
import pyvista as pv
from pyvistaqt import QtInteractor
from PySide6.QtWidgets import QFrame, QVBoxLayout

from .models import BeamData, HorizontalPressureIntensityField, ModelGeometry, ModelResultField, NodeData
from .section_profiles import beam_section_color, beam_section_family


class Viewport3D(QFrame):
    _BOX_FACE_TEMPLATE = np.array(
        [
            [4, 0, 1, 2, 3],
            [4, 4, 5, 6, 7],
            [4, 0, 1, 5, 4],
            [4, 1, 2, 6, 5],
            [4, 2, 3, 7, 6],
            [4, 3, 0, 4, 7],
        ],
        dtype=np.int32,
    )
    _MODEL_LENGTH_TO_MM = {
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
    }
    _SCALAR_BAR_VERTICAL = "Vertical"
    _SCALAR_BAR_HORIZONTAL = "Horizontal"
    _SCALAR_BAR_STYLE_APP = "App"
    _SCALAR_BAR_STYLE_PYVISTA = "PyVista Default"
    _VERTICAL_SCALAR_BAR_POSITION_X = 0.915
    _VERTICAL_SCALAR_BAR_POSITION_Y = 0.12
    _VERTICAL_SCALAR_BAR_WIDTH = 0.035
    _VERTICAL_SCALAR_BAR_HEIGHT = 0.66
    _HORIZONTAL_SCALAR_BAR_POSITION_X = 0.22
    _HORIZONTAL_SCALAR_BAR_POSITION_Y = 0.065
    _HORIZONTAL_SCALAR_BAR_WIDTH = 0.56
    _HORIZONTAL_SCALAR_BAR_HEIGHT = 0.075
    _PYVISTA_VERTICAL_SCALAR_BAR_POSITION_X = 0.915
    _PYVISTA_VERTICAL_SCALAR_BAR_POSITION_Y = 0.12
    _PYVISTA_VERTICAL_SCALAR_BAR_WIDTH = 0.035
    _PYVISTA_VERTICAL_SCALAR_BAR_HEIGHT = 0.66
    _PYVISTA_HORIZONTAL_SCALAR_BAR_POSITION_X = 0.22
    _PYVISTA_HORIZONTAL_SCALAR_BAR_POSITION_Y = 0.02
    _PYVISTA_HORIZONTAL_SCALAR_BAR_WIDTH = 0.56
    _PYVISTA_HORIZONTAL_SCALAR_BAR_HEIGHT = 0.075
    _PYVISTA_VERTICAL_TITLE_POSITION_X = 0.905
    _PYVISTA_VERTICAL_TITLE_POSITION_Y = 0.805
    _PYVISTA_HORIZONTAL_TITLE_POSITION_X = 0.22
    _PYVISTA_HORIZONTAL_TITLE_POSITION_Y = 0.10
    _PYVISTA_SCALAR_BAR_TITLE_FONT_SIZE = 11
    _PYVISTA_SCALAR_BAR_LABEL_FONT_SIZE = 18
    _PYVISTA_HORIZONTAL_LABEL_TEXT_PAD = 6
    _PYVISTA_VERTICAL_LABEL_TEXT_PAD = 6
    _SCALAR_BAR_ARGS = {
        "title": "",
        "n_labels": 5,
        "title_font_size": 18,
        "label_font_size": 20,
        "unconstrained_font_size": True,
    }

    def __init__(self, parent=None) -> None:
        super().__init__(parent)
        self.setFrameShape(QFrame.NoFrame)
        self.setObjectName("viewportFrame")

        layout = QVBoxLayout(self)
        layout.setContentsMargins(0, 0, 0, 0)

        self.plotter = QtInteractor(self)
        layout.addWidget(self.plotter.interactor)

        self._current_model: ModelGeometry | None = None
        self._current_points = np.empty((0, 3), dtype=float)
        self._current_node_ids: list[int] = []
        self._node_index_by_id: dict[int, int] = {}
        self._beam_segments = np.empty((0, 2, 3), dtype=float)
        self._beam_lookup: list[BeamData] = []
        self._beam_mesh = pv.PolyData()
        self._node_actor = None
        self._beam_actor = None
        self._beam_highlight_actor = None
        self._joint_highlight_actor = None
        self._beam_highlight_mesh = pv.PolyData()
        self._joint_highlight_mesh = pv.PolyData()
        self._show_joints = True
        self._show_grid = True
        self._show_extrude = False
        self._beam_color_mode = "Default"
        self._background_mode = "Dark"
        self._isolate_selected_beam = False
        self._up_axis = "Y"
        self._current_result_field: ModelResultField | HorizontalPressureIntensityField | None = None
        self._display_component = "Wireframe"
        self._deformation_scale = 1.0
        self._color_map = "jet"
        self._scalar_bar_orientation = self._SCALAR_BAR_VERTICAL
        self._scalar_bar_style = self._SCALAR_BAR_STYLE_APP
        self._scalar_limits_override: tuple[float, float] | None = None
        self._overlay_text: str | None = None
        self._selected_beam_id: int | None = None
        self._selected_joint_id: int | None = None
        self._on_beam_selected = None
        self._on_joint_selected = None

        self._configure_scene()
        self._configure_orientation_widget()
        self._configure_default_selection()

    def _configure_scene(self) -> None:
        if self._background_mode == "White":
            self.plotter.set_background("#f8fafc")
            grid_color = "#cbd5e1"
        else:
            self.plotter.set_background("#02050d")
            grid_color = "#18324f"
        if self._show_grid:
            self.plotter.show_grid(color=grid_color)
        self.plotter.enable_anti_aliasing("msaa")

    def _overlay_primary_color(self) -> str:
        return "#0f172a" if self._background_mode == "White" else "#8ff6ff"

    def _overlay_muted_color(self) -> str:
        return "#475569" if self._background_mode == "White" else "#94a3b8"

    def _overlay_label_color(self) -> str:
        return "#111827" if self._background_mode == "White" else "#dbeafe"

    def _pressure_context_beam_color(self) -> str:
        return "#56606b" if self._background_mode == "White" else "#3a4350"

    def _excluded_result_beam_color(self) -> str:
        return "#64748b" if self._background_mode == "White" else "#475569"

    def _pressure_context_node_color(self) -> str:
        return "#64748b" if self._background_mode == "White" else "#4b5563"

    def _configure_orientation_widget(self) -> None:
        self.plotter.add_camera_orientation_widget()

    def _configure_default_selection(self) -> None:
        self.plotter.enable_point_picking(
            callback=self._handle_pick_point,
            left_clicking=True,
            show_message=False,
            show_point=False,
            clear_on_no_selection=False,
            picker="hardware",
        )

    def show_placeholder(self) -> None:
        self._current_result_field = None
        self._display_component = "Wireframe"
        self.plotter.clear()
        self._configure_scene()
        self.plotter.add_text(
            "Connect to STAAD to load nodes and beams.",
            position="upper_left",
            font_size=12,
            color=self._overlay_primary_color(),
        )
        self.plotter.render()

    def set_selection_callbacks(self, on_beam_selected=None, on_joint_selected=None) -> None:
        self._on_beam_selected = on_beam_selected
        self._on_joint_selected = on_joint_selected

    def render_model(
        self,
        model: ModelGeometry,
        result_field: ModelResultField | HorizontalPressureIntensityField | None = None,
        display_component: str = "Wireframe",
        deformation_scale: float = 1.0,
        color_map: str = "jet",
        scalar_bar_orientation: str = _SCALAR_BAR_VERTICAL,
        scalar_limits_override: tuple[float, float] | None = None,
        overlay_text: str | None = None,
        reset_camera: bool = True,
    ) -> None:
        self._current_model = model
        self._current_result_field = result_field
        self._display_component = display_component
        self._deformation_scale = deformation_scale
        self._color_map = color_map
        self._scalar_bar_orientation = self._normalize_scalar_bar_orientation(scalar_bar_orientation)
        self._scalar_limits_override = scalar_limits_override
        self._overlay_text = overlay_text
        self._up_axis = model.up_axis
        self.plotter.clear()
        self._configure_scene()
        self._beam_highlight_actor = None
        self._joint_highlight_actor = None

        node_ids = sorted(model.nodes)
        base_points = np.array(
            [[model.nodes[node_id].x, model.nodes[node_id].y, model.nodes[node_id].z] for node_id in node_ids],
            dtype=float,
        )
        points = self._apply_result_deformation(node_ids, base_points)
        self._current_points = points
        self._current_node_ids = node_ids
        node_index_by_id = {node_id: index for index, node_id in enumerate(node_ids)}
        self._node_index_by_id = node_index_by_id
        clim = scalar_limits_override or self._scalar_limits(result_field)
        is_pressure_intensity = isinstance(result_field, HorizontalPressureIntensityField)
        visible_beams = self._visible_beams(model)
        excluded_beam_ids = result_field.excluded_beam_ids if isinstance(result_field, ModelResultField) else set()
        visible_node_ids = self._visible_node_ids(visible_beams, excluded_beam_ids)

        if len(points) > 0 and self._show_joints:
            visible_indexes = [node_index_by_id[node_id] for node_id in visible_node_ids if node_id in node_index_by_id]
            visible_points = points[visible_indexes] if visible_indexes else np.empty((0, 3), dtype=float)
            visible_node_values = [node_ids[index] for index in visible_indexes]
            if not visible_node_values:
                self._node_actor = None
            else:
                node_cloud = pv.PolyData(visible_points)
                node_cloud["node_id"] = np.array(visible_node_values, dtype=np.int32)
                node_kwargs = {
                    "point_size": 4 if is_pressure_intensity else 7,
                    "render_points_as_spheres": True,
                    "pickable": True,
                }
                if (
                    result_field is not None
                    and display_component != "Wireframe"
                    and not is_pressure_intensity
                    and result_field.node_values
                ):
                    node_cloud["node_value"] = np.array(
                        [result_field.node_values.get(node_id, 0.0) for node_id in visible_node_values],
                        dtype=float,
                    )
                    self._node_actor = self.plotter.add_mesh(
                        node_cloud,
                        scalars="node_value",
                        cmap=self._color_map,
                        clim=clim,
                        show_scalar_bar=False,
                        **node_kwargs,
                    )
                else:
                    if is_pressure_intensity:
                        self._node_actor = self.plotter.add_mesh(
                            node_cloud,
                            color=self._pressure_context_node_color(),
                            opacity=0.34,
                            **node_kwargs,
                        )
                    else:
                        self._node_actor = self.plotter.add_mesh(node_cloud, color="#facc15", **node_kwargs)
        else:
            self._node_actor = None

        line_cells: list[int] = []
        self._beam_lookup = []
        beam_segments: list[np.ndarray] = []
        beam_scalars: list[float] = []
        excluded_beam_ids = result_field.excluded_beam_ids if isinstance(result_field, ModelResultField) else set()
        for beam in visible_beams:
            start_index = node_index_by_id.get(beam.start_node_id)
            end_index = node_index_by_id.get(beam.end_node_id)
            if start_index is None or end_index is None:
                continue
            line_cells.extend((2, start_index, end_index))
            self._beam_lookup.append(beam)
            beam_segments.append(np.array((points[start_index], points[end_index]), dtype=float))
            if result_field is not None and not is_pressure_intensity:
                beam_scalars.append(
                    math.nan if beam.beam_id in excluded_beam_ids else result_field.beam_values.get(beam.beam_id, math.nan)
                )

        self._beam_segments = np.array(beam_segments, dtype=float) if beam_segments else np.empty((0, 2, 3), dtype=float)

        if line_cells and len(points) > 0:
            if self._show_extrude:
                beam_mesh = self._build_extruded_mesh(
                    points, node_index_by_id, visible_beams, beam_scalars, result_field, display_component
                )
                self._beam_mesh = beam_mesh
                beam_kwargs = {"pickable": True}
                if (
                    result_field is not None
                    and display_component != "Wireframe"
                    and not is_pressure_intensity
                    and "beam_value" in beam_mesh.cell_data
                ):
                    self._beam_actor = self.plotter.add_mesh(
                        beam_mesh,
                        scalars="beam_value",
                        cmap=self._color_map,
                        clim=clim,
                        scalar_bar_args=self._scalar_bar_args(result_field),
                        nan_color=self._excluded_result_beam_color(),
                        nan_opacity=0.55,
                        **beam_kwargs,
                    )
                elif (
                    result_field is not None
                    and display_component != "Wireframe"
                    and not is_pressure_intensity
                    and "node_value" in beam_mesh.point_data
                ):
                    self._beam_actor = self.plotter.add_mesh(
                        beam_mesh,
                        scalars="node_value",
                        cmap=self._color_map,
                        clim=clim,
                        scalar_bar_args=self._scalar_bar_args(result_field),
                        nan_color=self._excluded_result_beam_color(),
                        nan_opacity=0.55,
                        **beam_kwargs,
                    )
                elif is_pressure_intensity:
                    self._beam_actor = self.plotter.add_mesh(
                        beam_mesh,
                        color=self._pressure_context_beam_color(),
                        opacity=0.36,
                        show_edges=False,
                        **beam_kwargs,
                    )
                elif self._beam_color_mode == "Section Type" and "section_rgb" in beam_mesh.cell_data:
                    self._beam_actor = self.plotter.add_mesh(
                        beam_mesh,
                        scalars="section_rgb",
                        rgb=True,
                        **beam_kwargs,
                    )
                else:
                    self._beam_actor = self.plotter.add_mesh(beam_mesh, color="#16d9e3", **beam_kwargs)
            else:
                beam_mesh = pv.PolyData()
                beam_mesh.points = points
                beam_mesh.lines = np.array(line_cells, dtype=np.int32)
                beam_mesh.cell_data["beam_id"] = np.array([beam.beam_id for beam in self._beam_lookup], dtype=np.int32)
                beam_kwargs = {
                    "line_width": 1.25 if is_pressure_intensity else 1.8,
                    "render_lines_as_tubes": False,
                    "pickable": True,
                }
                if (
                    result_field is not None
                    and display_component != "Wireframe"
                    and not is_pressure_intensity
                    and beam_scalars
                ):
                    beam_mesh.cell_data["beam_value"] = np.array(beam_scalars, dtype=float)
                    self._beam_actor = self.plotter.add_mesh(
                        beam_mesh,
                        scalars="beam_value",
                        cmap=self._color_map,
                        clim=clim,
                        scalar_bar_args=self._scalar_bar_args(result_field),
                        nan_color=self._excluded_result_beam_color(),
                        nan_opacity=0.55,
                        **beam_kwargs,
                    )
                elif (
                    result_field is not None
                    and display_component != "Wireframe"
                    and not is_pressure_intensity
                    and result_field.node_values
                ):
                    beam_mesh.point_data["node_value"] = np.array(
                        [result_field.node_values.get(node_id, 0.0) for node_id in node_ids],
                        dtype=float,
                    )
                    self._beam_actor = self.plotter.add_mesh(
                        beam_mesh,
                        scalars="node_value",
                        cmap=self._color_map,
                        clim=clim,
                        scalar_bar_args=self._scalar_bar_args(result_field),
                        **beam_kwargs,
                    )
                elif is_pressure_intensity:
                    self._beam_actor = self.plotter.add_mesh(
                        beam_mesh,
                        color=self._pressure_context_beam_color(),
                        opacity=0.58,
                        **beam_kwargs,
                    )
                elif self._beam_color_mode == "Section Type" and self._beam_lookup:
                    beam_mesh.cell_data["section_rgb"] = np.array(
                        [self._section_color_rgb(beam) for beam in self._beam_lookup],
                        dtype=np.uint8,
                    )
                    self._beam_actor = self.plotter.add_mesh(
                        beam_mesh,
                        scalars="section_rgb",
                        rgb=True,
                        **beam_kwargs,
                    )
                else:
                    self._beam_actor = self.plotter.add_mesh(beam_mesh, color="#16d9e3", **beam_kwargs)
                self._beam_mesh = beam_mesh
        else:
            self._beam_actor = None
            self._beam_mesh = pv.PolyData()

        if is_pressure_intensity:
            self._render_pressure_intensity_plane(model, result_field, clim)

        self._add_view_overlay()
        self._add_scalar_bar_overlays(result_field, clim)

        if reset_camera:
            self.plotter.reset_camera()
            self.apply_up_axis(self._up_axis, render=False)

        self._ensure_highlight_actors()
        self._restore_selection(render=False)
        self.plotter.render()

    def _render_pressure_intensity_plane(
        self,
        model: ModelGeometry,
        result_field: HorizontalPressureIntensityField,
        clim: tuple[float, float] | None,
    ) -> None:
        if result_field.boundary is None or not result_field.node_values:
            return

        a_min, a_max, b_min, b_max = result_field.boundary
        grid_count = 220
        da = (a_max - a_min) / grid_count
        db = (b_max - b_min) / grid_count
        if da <= 0.0 or db <= 0.0:
            return

        support_points: list[tuple[float, float, int, np.ndarray]] = []
        for node_id in result_field.node_values:
            node = model.nodes.get(node_id)
            if node is None:
                continue
            if result_field.up_axis.upper() == "Y":
                a, b = node.x, node.z
                point = np.array([node.x, result_field.elevation, node.z], dtype=float)
            else:
                a, b = node.x, node.y
                point = np.array([node.x, node.y, result_field.elevation], dtype=float)
            support_points.append((a, b, node_id, point))
        if not support_points:
            return

        if result_field.zone_mode == "Column Zones":
            self._render_pressure_intensity_zones(result_field, support_points, clim)
            return

        points: list[tuple[float, float, float]] = []
        for ib in range(grid_count + 1):
            b = b_min + ib * db
            for ia in range(grid_count + 1):
                a = a_min + ia * da
                if result_field.up_axis.upper() == "Y":
                    points.append((a, result_field.elevation, b))
                else:
                    points.append((a, b, result_field.elevation))

        scalar_grid = np.empty((grid_count + 1, grid_count + 1), dtype=float)
        for ib in range(grid_count + 1):
            b = b_min + ib * db
            for ia in range(grid_count + 1):
                a = a_min + ia * da
                scalar_grid[ib, ia] = self._pressure_visual_value(a, b, support_points, result_field)
        scalar_grid = self._smooth_pressure_grid(scalar_grid, iterations=3)

        faces: list[int] = []
        for ib in range(grid_count):
            for ia in range(grid_count):
                p0 = ib * (grid_count + 1) + ia
                p1 = p0 + 1
                p2 = p0 + grid_count + 2
                p3 = p0 + grid_count + 1
                faces.extend((4, p0, p1, p2, p3))

        mesh = pv.PolyData(np.array(points, dtype=float), faces=np.array(faces, dtype=np.int32))
        mesh.point_data["pressure_intensity"] = scalar_grid.reshape(-1)
        self.plotter.add_mesh(
            mesh,
            scalars="pressure_intensity",
            cmap=self._color_map,
            clim=clim,
            opacity=0.68,
            show_edges=False,
            interpolate_before_map=True,
            scalar_bar_args=self._scalar_bar_args(result_field),
            pickable=False,
            render=False,
        )

        self._add_pressure_support_points(result_field, support_points, clim)
        if len(support_points) <= 80:
            self.plotter.add_point_labels(
                np.array([item[3] for item in support_points], dtype=float),
                [str(item[2]) for item in support_points],
                font_size=9,
                text_color=self._overlay_label_color(),
                show_points=False,
                render=False,
            )

    def _render_pressure_intensity_zones(
        self,
        result_field: HorizontalPressureIntensityField,
        support_points: list[tuple[float, float, int, np.ndarray]],
        clim: tuple[float, float] | None,
    ) -> None:
        radius = result_field.zone_radius
        if radius <= 0.0:
            return

        a_min, a_max, b_min, b_max = result_field.boundary or (0.0, 0.0, 0.0, 0.0)
        span = max(a_max - a_min, b_max - b_min, radius * 2.0)
        grid_count = max(220, min(360, int(span / max(radius / 40.0, 1e-9))))
        da = (a_max - a_min) / grid_count
        db = (b_max - b_min) / grid_count
        if da <= 0.0 or db <= 0.0:
            return

        points: list[tuple[float, float, float]] = []
        for ib in range(grid_count + 1):
            b = b_min + ib * db
            for ia in range(grid_count + 1):
                a = a_min + ia * da
                if result_field.up_axis.upper() == "Y":
                    points.append((a, result_field.elevation, b))
                else:
                    points.append((a, b, result_field.elevation))

        scalar_grid = np.empty((grid_count + 1, grid_count + 1), dtype=float)
        for ib in range(grid_count + 1):
            b = b_min + ib * db
            for ia in range(grid_count + 1):
                a = a_min + ia * da
                scalar_grid[ib, ia] = self._pressure_visual_value(a, b, support_points, result_field)
        scalar_grid = self._smooth_pressure_grid(scalar_grid, iterations=5)

        faces: list[int] = []
        for ib in range(grid_count):
            center_b = b_min + (ib + 0.5) * db
            for ia in range(grid_count):
                center_a = a_min + (ia + 0.5) * da
                nearest_distance_sq = min(
                    (item[0] - center_a) ** 2 + (item[1] - center_b) ** 2
                    for item in support_points
                )
                if nearest_distance_sq > radius * radius:
                    continue

                p0 = ib * (grid_count + 1) + ia
                p1 = p0 + 1
                p2 = p0 + grid_count + 2
                p3 = p0 + grid_count + 1
                faces.extend((4, p0, p1, p2, p3))

        if not faces:
            return

        mesh = pv.PolyData(np.array(points, dtype=float), faces=np.array(faces, dtype=np.int32))
        mesh.point_data["pressure_intensity"] = scalar_grid.reshape(-1)
        self.plotter.add_mesh(
            mesh,
            scalars="pressure_intensity",
            cmap=self._color_map,
            clim=clim,
            opacity=0.72,
            show_edges=False,
            interpolate_before_map=True,
            scalar_bar_args=self._scalar_bar_args(result_field),
            pickable=False,
            render=False,
        )
        self._add_pressure_support_points(result_field, support_points, clim)

    def _smooth_pressure_grid(self, values: np.ndarray, iterations: int) -> np.ndarray:
        if values.size == 0 or iterations <= 0:
            return values

        smoothed = np.array(values, dtype=float, copy=True)
        for _ in range(iterations):
            padded = np.pad(smoothed, 1, mode="edge")
            smoothed = (
                padded[1:-1, 1:-1] * 4.0
                + padded[:-2, 1:-1] * 2.0
                + padded[2:, 1:-1] * 2.0
                + padded[1:-1, :-2] * 2.0
                + padded[1:-1, 2:] * 2.0
                + padded[:-2, :-2]
                + padded[:-2, 2:]
                + padded[2:, :-2]
                + padded[2:, 2:]
            ) / 16.0
        return smoothed

    def _add_pressure_support_points(
        self,
        result_field: HorizontalPressureIntensityField,
        support_points: list[tuple[float, float, int, np.ndarray]],
        clim: tuple[float, float] | None,
    ) -> None:
        if not support_points:
            return

        support_poly = pv.PolyData(np.array([item[3] for item in support_points], dtype=float))
        support_poly["pressure"] = np.array(
            [result_field.node_values.get(item[2], 0.0) for item in support_points],
            dtype=float,
        )
        self.plotter.add_mesh(
            support_poly,
            scalars="pressure",
            cmap=self._color_map,
            clim=clim,
            show_scalar_bar=False,
            point_size=8,
            render_points_as_spheres=True,
            pickable=False,
            render=False,
        )

    def _pressure_visual_value(
        self,
        a: float,
        b: float,
        support_points: list[tuple[float, float, int, np.ndarray]],
        result_field: HorizontalPressureIntensityField,
    ) -> float:
        distances = [
            ((item[0] - a) ** 2 + (item[1] - b) ** 2, item[2])
            for item in support_points
        ]
        distances.sort(key=lambda item: item[0])
        if not distances:
            return 0.0
        if distances[0][0] < 1e-18:
            return result_field.node_values.get(distances[0][1], 0.0)

        neighbors = distances[: min(4, len(distances))]
        weights = [1.0 / max(distance, 1e-18) for distance, _ in neighbors]
        total_weight = sum(weights)
        if total_weight <= 0.0:
            return 0.0
        return sum(
            weight * result_field.node_values.get(node_id, 0.0)
            for weight, (_, node_id) in zip(weights, neighbors)
        ) / total_weight

    def _build_extruded_mesh(
        self,
        points: np.ndarray,
        node_index_by_id: dict[int, int],
        beams: list[BeamData],
        beam_scalars: list[float],
        result_field: ModelResultField | None,
        display_component: str,
    ) -> pv.PolyData:
        has_node_values = result_field is not None and display_component != "Wireframe" and bool(result_field.node_values)
        has_beam_values = result_field is not None and display_component != "Wireframe" and bool(beam_scalars)
        default_size = self._default_extrude_size()
        mesh_points: list[np.ndarray] = []
        mesh_faces: list[np.ndarray] = []
        beam_value_blocks: list[np.ndarray] = []
        node_value_blocks: list[np.ndarray] = []
        beam_id_blocks: list[np.ndarray] = []
        section_rgb_blocks: list[np.ndarray] = []
        point_offset = 0

        for index, beam in enumerate(beams):
            start_idx = node_index_by_id.get(beam.start_node_id)
            end_idx = node_index_by_id.get(beam.end_node_id)
            if start_idx is None or end_idx is None:
                continue

            p1 = points[start_idx]
            p2 = points[end_idx]
            dx = p2 - p1
            length = np.linalg.norm(dx)
            if length < 1e-6:
                continue

            d = beam.depth or default_size
            w = beam.width or default_size

            axis_x = dx / length
            axis_y, axis_z = self._section_axes(axis_x, beam.beta_angle)
            beam_value = beam_scalars[index] if has_beam_values and len(beam_scalars) > index else None
            start_node_value = None
            end_node_value = None
            if has_node_values:
                start_node_value = result_field.node_values.get(beam.start_node_id, 0.0)
                end_node_value = result_field.node_values.get(beam.end_node_id, 0.0)

            point_offset = self._append_beam_section_mesh(
                mesh_points,
                mesh_faces,
                beam_value_blocks,
                node_value_blocks,
                beam_id_blocks,
                section_rgb_blocks,
                point_offset,
                beam,
                p1,
                p2,
                axis_y,
                axis_z,
                d,
                w,
                beam_value,
                start_node_value,
                end_node_value,
            )

        if not mesh_points:
            return pv.PolyData()

        mesh = pv.PolyData(
            np.vstack(mesh_points),
            faces=np.concatenate(mesh_faces),
        )
        if has_beam_values and beam_value_blocks:
            mesh.cell_data["beam_value"] = np.concatenate(beam_value_blocks)
        if has_node_values and node_value_blocks:
            mesh.point_data["node_value"] = np.concatenate(node_value_blocks)
        if beam_id_blocks:
            mesh.cell_data["beam_id"] = np.concatenate(beam_id_blocks)
        if section_rgb_blocks:
            mesh.cell_data["section_rgb"] = np.vstack(section_rgb_blocks).astype(np.uint8)
        return mesh

    def _visible_beams(self, model: ModelGeometry) -> list[BeamData]:
        if not self._isolate_selected_beam or self._selected_beam_id is None:
            return list(model.beams)
        for beam in model.beams:
            if beam.beam_id == self._selected_beam_id:
                return [beam]
        return list(model.beams)

    def _visible_node_ids(self, beams: list[BeamData], excluded_beam_ids: set[int] | None = None) -> list[int]:
        excluded_beam_ids = excluded_beam_ids or set()
        active_beams = [beam for beam in beams if beam.beam_id not in excluded_beam_ids]
        if not self._isolate_selected_beam and not excluded_beam_ids:
            return list(self._current_node_ids)
        if not active_beams:
            return []
        node_ids: list[int] = []
        for beam in active_beams:
            node_ids.extend((beam.start_node_id, beam.end_node_id))
        return sorted(set(node_ids))

    def _restore_selection(self, render: bool = True) -> None:
        if self._current_model is None:
            return

        if self._selected_beam_id is not None:
            beam = next((item for item in self._current_model.beams if item.beam_id == self._selected_beam_id), None)
            if beam is not None:
                self._highlight_beam(beam, render=render, notify=False)
                return

        if self._selected_joint_id is not None:
            node = self._current_model.nodes.get(self._selected_joint_id)
            if node is not None:
                self._highlight_joint(node, render=render, notify=False)
                return

        self.clear_selection(render=render)

    def _section_color_rgb(self, beam: BeamData) -> np.ndarray:
        color = beam_section_color(beam).lstrip("#")
        return np.array([int(color[index:index + 2], 16) for index in (0, 2, 4)], dtype=np.uint8)

    def _append_beam_section_mesh(
        self,
        mesh_points: list[np.ndarray],
        mesh_faces: list[np.ndarray],
        beam_value_blocks: list[np.ndarray],
        node_value_blocks: list[np.ndarray],
        beam_id_blocks: list[np.ndarray],
        section_rgb_blocks: list[np.ndarray],
        point_offset: int,
        beam: BeamData,
        p1: np.ndarray,
        p2: np.ndarray,
        axis_y: np.ndarray,
        axis_z: np.ndarray,
        depth: float,
        width: float,
        beam_value: float | None,
        start_node_value: float | None,
        end_node_value: float | None,
    ) -> int:
        section_rgb = self._section_color_rgb(beam)

        if beam.section_property_type == 671:
            diameter = beam.section_property_values[4] if len(beam.section_property_values) > 4 and beam.section_property_values[4] > 0.0 else max(depth, width)
            profile = self._regular_polygon_profile(diameter * 0.5, diameter * 0.5, sides=24)
            point_offset, cell_count = self._append_profile_mesh(
                mesh_points,
                mesh_faces,
                point_offset,
                p1,
                p2,
                axis_y,
                axis_z,
                profile,
                profile,
            )
            self._append_section_metadata(
                beam_value_blocks,
                node_value_blocks,
                beam_id_blocks,
                section_rgb_blocks,
                beam.beam_id,
                section_rgb,
                cell_count,
                profile.shape[0] * 2,
                beam_value,
                start_node_value,
                end_node_value,
            )
            return point_offset

        if beam.section_property_type == 674:
            values = beam.section_property_values
            section_depth = values[4] if len(values) > 4 and values[4] > 0.0 else depth
            top_width = values[5] if len(values) > 5 and values[5] > 0.0 else width
            bottom_width = values[6] if len(values) > 6 and values[6] > 0.0 else top_width
            profile = self._trapezoid_profile(section_depth, top_width, bottom_width)
            point_offset, cell_count = self._append_profile_mesh(
                mesh_points,
                mesh_faces,
                point_offset,
                p1,
                p2,
                axis_y,
                axis_z,
                profile,
                profile,
            )
            self._append_section_metadata(
                beam_value_blocks,
                node_value_blocks,
                beam_id_blocks,
                section_rgb_blocks,
                beam.beam_id,
                section_rgb,
                cell_count,
                profile.shape[0] * 2,
                beam_value,
                start_node_value,
                end_node_value,
            )
            return point_offset

        if beam.section_property_type == 695:
            values = beam.section_property_values
            outer_diameter = values[1] if len(values) > 1 and values[1] > 0.0 else max(depth, width)
            inner_diameter = values[2] if len(values) > 2 and values[2] > 0.0 else outer_diameter * 0.7
            outer_profile = self._regular_polygon_profile(outer_diameter * 0.5, outer_diameter * 0.5, sides=24)
            inner_profile = self._regular_polygon_profile(inner_diameter * 0.5, inner_diameter * 0.5, sides=24)
            point_offset, cell_count = self._append_profile_mesh(
                mesh_points,
                mesh_faces,
                point_offset,
                p1,
                p2,
                axis_y,
                axis_z,
                outer_profile,
                outer_profile,
                inner_profile,
                inner_profile,
            )
            self._append_section_metadata(
                beam_value_blocks,
                node_value_blocks,
                beam_id_blocks,
                section_rgb_blocks,
                beam.beam_id,
                section_rgb,
                cell_count,
                outer_profile.shape[0] * 2 + inner_profile.shape[0] * 2,
                beam_value,
                start_node_value,
                end_node_value,
            )
            return point_offset

        if beam.section_property_type == 675:
            values = beam.section_property_values
            start_diameter = values[4] if len(values) > 4 and values[4] > 0.0 else max(depth, width)
            end_diameter = values[5] if len(values) > 5 and values[5] > 0.0 else start_diameter
            thickness = values[6] if len(values) > 6 and values[6] > 0.0 else min(start_diameter, end_diameter) * 0.12
            section_type = int(round(values[7])) if len(values) > 7 and values[7] > 0.0 else 1
            sides = {1: 24, 2: 16, 3: 12, 4: 8, 5: 6, 6: 4}.get(section_type, 24)
            start_outer = self._regular_polygon_profile(start_diameter * 0.5, start_diameter * 0.5, sides=sides)
            end_outer = self._regular_polygon_profile(end_diameter * 0.5, end_diameter * 0.5, sides=sides)
            start_inner = self._regular_polygon_profile(max(start_diameter * 0.5 - thickness, 1e-6), max(start_diameter * 0.5 - thickness, 1e-6), sides=sides)
            end_inner = self._regular_polygon_profile(max(end_diameter * 0.5 - thickness, 1e-6), max(end_diameter * 0.5 - thickness, 1e-6), sides=sides)
            point_offset, cell_count = self._append_profile_mesh(
                mesh_points,
                mesh_faces,
                point_offset,
                p1,
                p2,
                axis_y,
                axis_z,
                start_outer,
                end_outer,
                start_inner,
                end_inner,
            )
            self._append_section_metadata(
                beam_value_blocks,
                node_value_blocks,
                beam_id_blocks,
                section_rgb_blocks,
                beam.beam_id,
                section_rgb,
                cell_count,
                start_outer.shape[0] * 2 + start_inner.shape[0] * 2,
                beam_value,
                start_node_value,
                end_node_value,
            )
            return point_offset

        prism_specs = self._beam_prism_specs(beam, depth, width)
        if not prism_specs:
            prism_specs = [(-depth * 0.5, depth * 0.5, -width * 0.5, width * 0.5, -depth * 0.5, depth * 0.5, -width * 0.5, width * 0.5)]

        for prism_spec in prism_specs:
            point_offset, cell_count = self._append_prism_mesh(
                mesh_points,
                mesh_faces,
                point_offset,
                p1,
                p2,
                axis_y,
                axis_z,
                prism_spec,
            )
            self._append_section_metadata(
                beam_value_blocks,
                node_value_blocks,
                beam_id_blocks,
                section_rgb_blocks,
                beam.beam_id,
                section_rgb,
                cell_count,
                8,
                beam_value,
                start_node_value,
                end_node_value,
            )
        return point_offset

    def _append_section_metadata(
        self,
        beam_value_blocks: list[np.ndarray],
        node_value_blocks: list[np.ndarray],
        beam_id_blocks: list[np.ndarray],
        section_rgb_blocks: list[np.ndarray],
        beam_id: int,
        section_rgb: np.ndarray,
        cell_count: int,
        point_count: int,
        beam_value: float | None,
        start_node_value: float | None,
        end_node_value: float | None,
    ) -> None:
        beam_id_blocks.append(np.full(cell_count, beam_id, dtype=np.int32))
        section_rgb_blocks.append(np.repeat(section_rgb[None, :], cell_count, axis=0))
        if beam_value is not None:
            beam_value_blocks.append(np.full(cell_count, beam_value, dtype=float))
        if start_node_value is not None and end_node_value is not None:
            half_count = point_count // 2
            node_value_blocks.append(
                np.concatenate(
                    (
                        np.full(half_count, start_node_value, dtype=float),
                        np.full(point_count - half_count, end_node_value, dtype=float),
                    )
                )
            )

    def _regular_polygon_profile(self, radius_y: float, radius_z: float, sides: int) -> np.ndarray:
        angles = np.linspace(0.0, 2.0 * np.pi, num=max(sides, 4), endpoint=False)
        return np.column_stack((np.sin(angles) * radius_y, np.cos(angles) * radius_z)).astype(float)

    def _trapezoid_profile(self, section_depth: float, top_width: float, bottom_width: float) -> np.ndarray:
        half_depth = section_depth * 0.5
        return np.array(
            [
                [half_depth, -top_width * 0.5],
                [half_depth, top_width * 0.5],
                [-half_depth, bottom_width * 0.5],
                [-half_depth, -bottom_width * 0.5],
            ],
            dtype=float,
        )

    def _append_profile_mesh(
        self,
        mesh_points: list[np.ndarray],
        mesh_faces: list[np.ndarray],
        point_offset: int,
        p1: np.ndarray,
        p2: np.ndarray,
        axis_y: np.ndarray,
        axis_z: np.ndarray,
        start_outer: np.ndarray,
        end_outer: np.ndarray,
        start_inner: np.ndarray | None = None,
        end_inner: np.ndarray | None = None,
    ) -> tuple[int, int]:
        start_outer_points = self._profile_points_3d(p1, axis_y, axis_z, start_outer)
        end_outer_points = self._profile_points_3d(p2, axis_y, axis_z, end_outer)
        point_blocks = [start_outer_points, end_outer_points]
        start_inner_offset = None
        end_inner_offset = None
        if start_inner is not None and end_inner is not None:
            start_inner_offset = start_outer.shape[0] * 2
            end_inner_offset = start_inner_offset + start_inner.shape[0]
            point_blocks.append(self._profile_points_3d(p1, axis_y, axis_z, start_inner))
            point_blocks.append(self._profile_points_3d(p2, axis_y, axis_z, end_inner))

        mesh_points.append(np.vstack(point_blocks))

        faces: list[np.ndarray] = []
        outer_start_offset = point_offset
        outer_end_offset = point_offset + start_outer.shape[0]
        faces.extend(self._profile_side_faces(outer_start_offset, outer_end_offset, start_outer.shape[0], invert=False))

        if start_inner_offset is not None and end_inner_offset is not None:
            inner_start_offset = point_offset + start_inner_offset
            inner_end_offset = point_offset + end_inner_offset
            faces.extend(self._profile_side_faces(inner_start_offset, inner_end_offset, start_inner.shape[0], invert=True))
            faces.extend(self._ring_cap_faces(outer_start_offset, inner_start_offset, start_outer.shape[0], invert=False))
            faces.extend(self._ring_cap_faces(outer_end_offset, inner_end_offset, start_outer.shape[0], invert=True))
        else:
            faces.extend(self._profile_cap_faces(outer_start_offset, start_outer.shape[0], invert=True))
            faces.extend(self._profile_cap_faces(outer_end_offset, start_outer.shape[0], invert=False))

        mesh_faces.extend(faces)
        new_point_offset = point_offset + sum(block.shape[0] for block in point_blocks)
        return new_point_offset, len(faces)

    def _profile_points_3d(
        self,
        origin: np.ndarray,
        axis_y: np.ndarray,
        axis_z: np.ndarray,
        profile: np.ndarray,
    ) -> np.ndarray:
        return origin + profile[:, 0:1] * axis_y[None, :] + profile[:, 1:2] * axis_z[None, :]

    def _profile_side_faces(self, start_offset: int, end_offset: int, count: int, invert: bool) -> list[np.ndarray]:
        faces: list[np.ndarray] = []
        for index in range(count):
            next_index = (index + 1) % count
            vertices = [start_offset + index, start_offset + next_index, end_offset + next_index, end_offset + index]
            if invert:
                vertices = [vertices[0], vertices[3], vertices[2], vertices[1]]
            faces.append(np.array([4, *vertices], dtype=np.int32))
        return faces

    def _profile_cap_faces(self, offset: int, count: int, invert: bool) -> list[np.ndarray]:
        faces: list[np.ndarray] = []
        for index in range(1, count - 1):
            vertices = [offset, offset + index, offset + index + 1]
            if invert:
                vertices = [vertices[0], vertices[2], vertices[1]]
            faces.append(np.array([3, *vertices], dtype=np.int32))
        return faces

    def _ring_cap_faces(self, outer_offset: int, inner_offset: int, count: int, invert: bool) -> list[np.ndarray]:
        faces: list[np.ndarray] = []
        for index in range(count):
            next_index = (index + 1) % count
            vertices = [outer_offset + index, outer_offset + next_index, inner_offset + next_index, inner_offset + index]
            if invert:
                vertices = [vertices[0], vertices[3], vertices[2], vertices[1]]
            faces.append(np.array([4, *vertices], dtype=np.int32))
        return faces

    def _beam_prism_specs(
        self,
        beam: BeamData,
        depth: float,
        width: float,
    ) -> list[tuple[float, float, float, float, float, float, float, float]]:
        prop_type = beam.section_property_type

        if prop_type == 680 and len(beam.section_property_values) >= 7:
            return self._tapered_i_prism_specs(beam, depth, width)

        if prop_type == 690 and len(beam.section_property_values) >= 12:
            return self._upt_wide_flange_prism_specs(beam, depth, width)

        if prop_type in {672, 699} and len(beam.section_property_values) >= 8:
            return self._rectangular_prism_specs_from_values(beam, depth, width)

        if prop_type in {673, 694}:
            return self._tee_prism_specs(beam, depth, width)

        if prop_type == 691:
            return self._channel_prism_specs(beam, depth, width)

        if prop_type == 692:
            return self._angle_prism_specs(beam, depth, width)

        if prop_type == 693:
            return self._double_angle_prism_specs(beam, depth, width)

        if prop_type == 696:
            return self._tube_prism_specs(beam, depth, width)

        if self._looks_like_i_section(beam):
            return self._wide_flange_prism_specs(
                start_depth=depth,
                web_thickness=beam.web_thickness,
                top_width_start=width,
                bottom_width_start=width,
                top_flange_start=beam.flange_thickness_top,
                bottom_flange_start=beam.flange_thickness_bottom,
            )

        return [(-depth * 0.5, depth * 0.5, -width * 0.5, width * 0.5, -depth * 0.5, depth * 0.5, -width * 0.5, width * 0.5)]

    def _looks_like_i_section(self, beam: BeamData) -> bool:
        if beam.web_thickness is None or beam.web_thickness <= 0.0:
            return False
        flange_top = beam.flange_thickness_top or beam.flange_thickness_bottom
        if flange_top is None or flange_top <= 0.0:
            return False

        label = f"{beam.section_display_name or ''} {beam.section_name or ''}".upper()
        excluded_tokens = ("PIPE", "TUBE", "ANGLE", "CHANNEL", "TEE", "RECT", "ROUND", "TRAP")
        if any(token in label for token in excluded_tokens):
            return False
        return True

    def _wide_flange_prism_specs(
        self,
        start_depth: float,
        web_thickness: float | None,
        top_width_start: float,
        bottom_width_start: float,
        top_flange_start: float | None,
        bottom_flange_start: float | None,
        end_depth: float | None = None,
        top_width_end: float | None = None,
        bottom_width_end: float | None = None,
        top_flange_end: float | None = None,
        bottom_flange_end: float | None = None,
    ) -> list[tuple[float, float, float, float, float, float, float, float]]:
        tw = max(web_thickness or min(top_width_start * 0.2, start_depth * 0.2), 1e-6)
        d1 = max(start_depth, 1e-6)
        d2 = max(end_depth if end_depth is not None else start_depth, 1e-6)
        w1_top = max(top_width_start, 1e-6)
        w1_bottom = max(bottom_width_start, 1e-6)
        w2_top = max(top_width_end if top_width_end is not None else top_width_start, 1e-6)
        w2_bottom = max(bottom_width_end if bottom_width_end is not None else bottom_width_start, 1e-6)
        tf1_top = min(max(top_flange_start or d1 * 0.1, 1e-6), d1 * 0.49)
        tf1_bottom = min(max(bottom_flange_start or tf1_top, 1e-6), d1 * 0.49)
        tf2_top = min(max(top_flange_end or tf1_top, 1e-6), d2 * 0.49)
        tf2_bottom = min(max(bottom_flange_end or tf1_bottom, 1e-6), d2 * 0.49)

        specs = [
            (
                d1 * 0.5 - tf1_top,
                d1 * 0.5,
                -w1_top * 0.5,
                w1_top * 0.5,
                d2 * 0.5 - tf2_top,
                d2 * 0.5,
                -w2_top * 0.5,
                w2_top * 0.5,
            ),
            (
                -d1 * 0.5,
                -d1 * 0.5 + tf1_bottom,
                -w1_bottom * 0.5,
                w1_bottom * 0.5,
                -d2 * 0.5,
                -d2 * 0.5 + tf2_bottom,
                -w2_bottom * 0.5,
                w2_bottom * 0.5,
            ),
        ]

        web_y1_min = -d1 * 0.5 + tf1_bottom
        web_y1_max = d1 * 0.5 - tf1_top
        web_y2_min = -d2 * 0.5 + tf2_bottom
        web_y2_max = d2 * 0.5 - tf2_top
        if web_y1_max > web_y1_min and web_y2_max > web_y2_min:
            specs.append(
                (
                    web_y1_min,
                    web_y1_max,
                    -tw * 0.5,
                    tw * 0.5,
                    web_y2_min,
                    web_y2_max,
                    -tw * 0.5,
                    tw * 0.5,
                )
            )
        return specs

    def _tapered_i_prism_specs(
        self,
        beam: BeamData,
        depth: float,
        width: float,
    ) -> list[tuple[float, float, float, float, float, float, float, float]]:
        values = beam.section_property_values
        start_depth = values[0] if len(values) > 0 and values[0] > 0.0 else depth
        web_thickness = values[1] if len(values) > 1 and values[1] > 0.0 else beam.web_thickness
        end_depth = values[2] if len(values) > 2 and values[2] > 0.0 else start_depth
        top_width = values[3] if len(values) > 3 and values[3] > 0.0 else width
        top_flange = values[4] if len(values) > 4 and values[4] > 0.0 else beam.flange_thickness_top
        bottom_width = values[5] if len(values) > 5 and values[5] > 0.0 else width
        bottom_flange = values[6] if len(values) > 6 and values[6] > 0.0 else beam.flange_thickness_bottom
        return self._wide_flange_prism_specs(
            start_depth=start_depth,
            web_thickness=web_thickness,
            top_width_start=top_width,
            bottom_width_start=bottom_width,
            top_flange_start=top_flange,
            bottom_flange_start=bottom_flange,
            end_depth=end_depth,
            top_width_end=top_width,
            bottom_width_end=bottom_width,
            top_flange_end=top_flange,
            bottom_flange_end=bottom_flange,
        )

    def _upt_wide_flange_prism_specs(
        self,
        beam: BeamData,
        depth: float,
        width: float,
    ) -> list[tuple[float, float, float, float, float, float, float, float]]:
        values = beam.section_property_values
        section_depth = values[1] if len(values) > 1 and values[1] > 0.0 else depth
        web_thickness = values[2] if len(values) > 2 and values[2] > 0.0 else beam.web_thickness
        top_width = values[3] if len(values) > 3 and values[3] > 0.0 else width
        top_flange = values[4] if len(values) > 4 and values[4] > 0.0 else beam.flange_thickness_top
        bottom_width = values[10] if len(values) > 10 and values[10] > 0.0 else top_width
        bottom_flange = values[11] if len(values) > 11 and values[11] > 0.0 else top_flange
        return self._wide_flange_prism_specs(
            start_depth=section_depth,
            web_thickness=web_thickness,
            top_width_start=top_width,
            bottom_width_start=bottom_width,
            top_flange_start=top_flange,
            bottom_flange_start=bottom_flange,
        )

    def _rectangular_prism_specs_from_values(
        self,
        beam: BeamData,
        depth: float,
        width: float,
    ) -> list[tuple[float, float, float, float, float, float, float, float]]:
        values = beam.section_property_values
        if beam.section_property_type == 699:
            section_depth = values[6] if len(values) > 6 and values[6] > 0.0 else depth
            section_width = values[7] if len(values) > 7 and values[7] > 0.0 else width
        else:
            section_depth = values[4] if len(values) > 4 and values[4] > 0.0 else depth
            section_width = values[5] if len(values) > 5 and values[5] > 0.0 else width
        return [(-section_depth * 0.5, section_depth * 0.5, -section_width * 0.5, section_width * 0.5, -section_depth * 0.5, section_depth * 0.5, -section_width * 0.5, section_width * 0.5)]

    def _tee_prism_specs(
        self,
        beam: BeamData,
        depth: float,
        width: float,
    ) -> list[tuple[float, float, float, float, float, float, float, float]]:
        values = beam.section_property_values
        if beam.section_property_type == 673 and len(values) >= 8:
            section_depth = values[4] if values[4] > 0.0 else depth
            flange_width = values[5] if values[5] > 0.0 else width
            stem_depth = values[6] if values[6] > 0.0 else section_depth * 0.8
            stem_width = values[7] if values[7] > 0.0 else beam.web_thickness or flange_width * 0.3
            flange_thickness = max(section_depth - stem_depth, 1e-6)
            web_thickness = stem_width
        else:
            section_depth = values[1] if len(values) > 1 and values[1] > 0.0 else depth
            flange_width = values[2] if len(values) > 2 and values[2] > 0.0 else width
            flange_thickness = values[3] if len(values) > 3 and values[3] > 0.0 else beam.flange_thickness_top or section_depth * 0.2
            web_thickness = values[4] if len(values) > 4 and values[4] > 0.0 else beam.web_thickness or flange_width * 0.3

        flange_thickness = min(flange_thickness, section_depth * 0.95)
        web_height = max(section_depth - flange_thickness, 1e-6)
        return [
            (
                section_depth * 0.5 - flange_thickness,
                section_depth * 0.5,
                -flange_width * 0.5,
                flange_width * 0.5,
                section_depth * 0.5 - flange_thickness,
                section_depth * 0.5,
                -flange_width * 0.5,
                flange_width * 0.5,
            ),
            (
                -section_depth * 0.5,
                -section_depth * 0.5 + web_height,
                -web_thickness * 0.5,
                web_thickness * 0.5,
                -section_depth * 0.5,
                -section_depth * 0.5 + web_height,
                -web_thickness * 0.5,
                web_thickness * 0.5,
            ),
        ]

    def _channel_prism_specs(
        self,
        beam: BeamData,
        depth: float,
        width: float,
    ) -> list[tuple[float, float, float, float, float, float, float, float]]:
        values = beam.section_property_values
        section_depth = values[1] if len(values) > 1 and values[1] > 0.0 else depth
        web_thickness = values[2] if len(values) > 2 and values[2] > 0.0 else beam.web_thickness or width * 0.25
        flange_width = values[3] if len(values) > 3 and values[3] > 0.0 else width
        flange_thickness = values[4] if len(values) > 4 and values[4] > 0.0 else beam.flange_thickness_top or section_depth * 0.12
        flange_thickness = min(flange_thickness, section_depth * 0.45)
        web_z_min = -flange_width * 0.5
        web_z_max = web_z_min + web_thickness
        return [
            (-section_depth * 0.5, section_depth * 0.5, web_z_min, web_z_max, -section_depth * 0.5, section_depth * 0.5, web_z_min, web_z_max),
            (section_depth * 0.5 - flange_thickness, section_depth * 0.5, web_z_min, flange_width * 0.5, section_depth * 0.5 - flange_thickness, section_depth * 0.5, web_z_min, flange_width * 0.5),
            (-section_depth * 0.5, -section_depth * 0.5 + flange_thickness, web_z_min, flange_width * 0.5, -section_depth * 0.5, -section_depth * 0.5 + flange_thickness, web_z_min, flange_width * 0.5),
        ]

    def _angle_prism_specs(
        self,
        beam: BeamData,
        depth: float,
        width: float,
    ) -> list[tuple[float, float, float, float, float, float, float, float]]:
        values = beam.section_property_values
        section_depth = values[1] if len(values) > 1 and values[1] > 0.0 else depth
        leg_width = values[2] if len(values) > 2 and values[2] > 0.0 else width
        thickness = values[3] if len(values) > 3 and values[3] > 0.0 else min(section_depth, leg_width) * 0.15
        thickness = min(thickness, min(section_depth, leg_width) * 0.49)
        return [
            (-section_depth * 0.5, section_depth * 0.5, -leg_width * 0.5, -leg_width * 0.5 + thickness, -section_depth * 0.5, section_depth * 0.5, -leg_width * 0.5, -leg_width * 0.5 + thickness),
            (section_depth * 0.5 - thickness, section_depth * 0.5, -leg_width * 0.5, leg_width * 0.5, section_depth * 0.5 - thickness, section_depth * 0.5, -leg_width * 0.5, leg_width * 0.5),
        ]

    def _double_angle_prism_specs(
        self,
        beam: BeamData,
        depth: float,
        width: float,
    ) -> list[tuple[float, float, float, float, float, float, float, float]]:
        values = beam.section_property_values
        section_depth = values[1] if len(values) > 1 and values[1] > 0.0 else depth
        leg_width = values[2] if len(values) > 2 and values[2] > 0.0 else width * 0.4
        thickness = values[3] if len(values) > 3 and values[3] > 0.0 else min(section_depth, leg_width) * 0.15
        spacing = values[4] if len(values) > 4 and values[4] > 0.0 else thickness
        thickness = min(thickness, min(section_depth, leg_width) * 0.49)
        half_gap = spacing * 0.5
        left_outer = -half_gap - leg_width
        left_inner = -half_gap
        right_inner = half_gap
        right_outer = half_gap + leg_width
        return [
            (-section_depth * 0.5, section_depth * 0.5, left_outer, left_outer + thickness, -section_depth * 0.5, section_depth * 0.5, left_outer, left_outer + thickness),
            (section_depth * 0.5 - thickness, section_depth * 0.5, left_outer, left_inner, section_depth * 0.5 - thickness, section_depth * 0.5, left_outer, left_inner),
            (-section_depth * 0.5, section_depth * 0.5, right_outer - thickness, right_outer, -section_depth * 0.5, section_depth * 0.5, right_outer - thickness, right_outer),
            (section_depth * 0.5 - thickness, section_depth * 0.5, right_inner, right_outer, section_depth * 0.5 - thickness, section_depth * 0.5, right_inner, right_outer),
        ]

    def _tube_prism_specs(
        self,
        beam: BeamData,
        depth: float,
        width: float,
    ) -> list[tuple[float, float, float, float, float, float, float, float]]:
        values = beam.section_property_values
        section_depth = values[1] if len(values) > 1 and values[1] > 0.0 else depth
        section_width = values[2] if len(values) > 2 and values[2] > 0.0 else width
        wall_thickness = values[3] if len(values) > 3 and values[3] > 0.0 else min(section_depth, section_width) * 0.12
        wall_thickness = min(wall_thickness, min(section_depth, section_width) * 0.45)
        return [
            (section_depth * 0.5 - wall_thickness, section_depth * 0.5, -section_width * 0.5, section_width * 0.5, section_depth * 0.5 - wall_thickness, section_depth * 0.5, -section_width * 0.5, section_width * 0.5),
            (-section_depth * 0.5, -section_depth * 0.5 + wall_thickness, -section_width * 0.5, section_width * 0.5, -section_depth * 0.5, -section_depth * 0.5 + wall_thickness, -section_width * 0.5, section_width * 0.5),
            (-section_depth * 0.5 + wall_thickness, section_depth * 0.5 - wall_thickness, -section_width * 0.5, -section_width * 0.5 + wall_thickness, -section_depth * 0.5 + wall_thickness, section_depth * 0.5 - wall_thickness, -section_width * 0.5, -section_width * 0.5 + wall_thickness),
            (-section_depth * 0.5 + wall_thickness, section_depth * 0.5 - wall_thickness, section_width * 0.5 - wall_thickness, section_width * 0.5, -section_depth * 0.5 + wall_thickness, section_depth * 0.5 - wall_thickness, section_width * 0.5 - wall_thickness, section_width * 0.5),
        ]

    def _append_prism_mesh(
        self,
        mesh_points: list[np.ndarray],
        mesh_faces: list[np.ndarray],
        point_offset: int,
        p1: np.ndarray,
        p2: np.ndarray,
        axis_y: np.ndarray,
        axis_z: np.ndarray,
        prism_spec: tuple[float, float, float, float, float, float, float, float],
    ) -> tuple[int, int]:
        (
            start_y_min,
            start_y_max,
            start_z_min,
            start_z_max,
            end_y_min,
            end_y_max,
            end_z_min,
            end_z_max,
        ) = prism_spec

        beam_points = np.array(
            [
                p1 + axis_y * start_y_min + axis_z * start_z_min,
                p1 + axis_y * start_y_max + axis_z * start_z_min,
                p1 + axis_y * start_y_max + axis_z * start_z_max,
                p1 + axis_y * start_y_min + axis_z * start_z_max,
                p2 + axis_y * end_y_min + axis_z * end_z_min,
                p2 + axis_y * end_y_max + axis_z * end_z_min,
                p2 + axis_y * end_y_max + axis_z * end_z_max,
                p2 + axis_y * end_y_min + axis_z * end_z_max,
            ],
            dtype=float,
        )
        mesh_points.append(beam_points)
        faces = (
            self._BOX_FACE_TEMPLATE
            + np.array([0, point_offset, point_offset, point_offset, point_offset], dtype=np.int32)
        ).reshape(-1)
        mesh_faces.append(faces)
        return point_offset + 8, 6

    def _section_axes(self, axis_x: np.ndarray, beta_angle: float) -> tuple[np.ndarray, np.ndarray]:
        reference = np.array([0.0, 0.0, 1.0], dtype=float)
        if abs(float(np.dot(axis_x, reference))) > 0.98:
            reference = np.array([0.0, 1.0, 0.0], dtype=float)

        axis_y = np.cross(reference, axis_x)
        axis_y_norm = np.linalg.norm(axis_y)
        if axis_y_norm < 1e-6:
            reference = np.array([1.0, 0.0, 0.0], dtype=float)
            axis_y = np.cross(reference, axis_x)
            axis_y_norm = np.linalg.norm(axis_y)
        axis_y = axis_y / axis_y_norm
        axis_z = np.cross(axis_x, axis_y)
        axis_z = axis_z / np.linalg.norm(axis_z)

        if abs(beta_angle) < 1e-9:
            return axis_y, axis_z

        angle_rad = np.deg2rad(beta_angle)
        cos_angle = float(np.cos(angle_rad))
        sin_angle = float(np.sin(angle_rad))
        rotated_y = axis_y * cos_angle + axis_z * sin_angle
        rotated_z = -axis_y * sin_angle + axis_z * cos_angle
        return rotated_y, rotated_z

    def _default_extrude_size(self) -> float:
        if self._current_model is None or not self._current_model.unit_length:
            return 0.3

        unit_to_mm = self._MODEL_LENGTH_TO_MM.get(self._current_model.unit_length.strip().upper(), 1.0)
        if unit_to_mm <= 0.0:
            return 0.3
        return 1000.0 / unit_to_mm * 0.3

    def _apply_result_deformation(self, node_ids: list[int], base_points: np.ndarray) -> np.ndarray:
        if (
            self._current_result_field is None
            or self._display_component == "Wireframe"
            or not self._current_result_field.node_values
            or len(base_points) == 0
        ):
            return base_points

        axis_index = {"DX": 0, "DY": 1, "DZ": 2}.get(self._display_component.upper())
        if axis_index is None:
            return base_points

        points = np.array(base_points, copy=True)
        for index, node_id in enumerate(node_ids):
            displacement = self._current_result_field.node_values.get(node_id, 0.0)
            points[index, axis_index] += (
                displacement
                * self._current_result_field.display_to_model_scale
                * self._deformation_scale
            )
        return points

    def _scalar_limits(
        self,
        result_field: ModelResultField | HorizontalPressureIntensityField | None,
    ) -> tuple[float, float] | None:
        if result_field is None:
            return None
        min_value = result_field.min_value
        max_value = result_field.max_value
        if min_value is None or max_value is None:
            return None
        if abs(max_value - min_value) < 1e-9:
            padding = max(abs(max_value) * 0.05, 1e-6)
            return (min_value - padding, max_value + padding)
        return (min_value, max_value)

    def _scalar_title(self, result_field: ModelResultField | HorizontalPressureIntensityField) -> str:
        suffix = f" ({result_field.unit})" if result_field.unit else ""
        return f"{result_field.component}{suffix}"

    def _normalize_scalar_bar_orientation(self, orientation: str | None) -> str:
        value = (orientation or self._SCALAR_BAR_VERTICAL).strip().lower()
        if value.startswith("h"):
            return self._SCALAR_BAR_HORIZONTAL
        return self._SCALAR_BAR_VERTICAL

    def _normalize_scalar_bar_style(self, style: str | None) -> str:
        value = (style or self._SCALAR_BAR_STYLE_APP).strip().lower()
        if value.startswith("pyvista"):
            return self._SCALAR_BAR_STYLE_PYVISTA
        return self._SCALAR_BAR_STYLE_APP

    def set_scalar_bar_style(self, style: str, rerender: bool = True) -> None:
        self._scalar_bar_style = self._normalize_scalar_bar_style(style)
        if rerender and self._current_model is not None:
            self.render_model(
                self._current_model,
                result_field=self._current_result_field,
                display_component=self._display_component,
                deformation_scale=self._deformation_scale,
                reset_camera=False,
                color_map=self._color_map,
                scalar_bar_orientation=self._scalar_bar_orientation,
                scalar_limits_override=self._scalar_limits_override,
                overlay_text=self._overlay_text,
            )

    def _scalar_bar_args(self, result_field: ModelResultField | HorizontalPressureIntensityField) -> dict[str, object]:
        if self._scalar_bar_style == self._SCALAR_BAR_STYLE_PYVISTA:
            args = {
                "title": "",
                "n_labels": 5,
                "title_font_size": self._PYVISTA_SCALAR_BAR_TITLE_FONT_SIZE,
                "label_font_size": self._PYVISTA_SCALAR_BAR_LABEL_FONT_SIZE,
                "unconstrained_font_size": True,
                "color": self._overlay_label_color(),
                "fmt": "%.3g",
            }
            if self._scalar_bar_orientation == self._SCALAR_BAR_HORIZONTAL:
                args.update(
                    {
                        "width": self._PYVISTA_HORIZONTAL_SCALAR_BAR_WIDTH,
                        "height": self._PYVISTA_HORIZONTAL_SCALAR_BAR_HEIGHT,
                        "position_x": self._PYVISTA_HORIZONTAL_SCALAR_BAR_POSITION_X,
                        "position_y": self._PYVISTA_HORIZONTAL_SCALAR_BAR_POSITION_Y,
                        "vertical": False,
                    }
                )
            else:
                args.update(
                    {
                        "width": self._PYVISTA_VERTICAL_SCALAR_BAR_WIDTH,
                        "height": self._PYVISTA_VERTICAL_SCALAR_BAR_HEIGHT,
                        "position_x": self._PYVISTA_VERTICAL_SCALAR_BAR_POSITION_X,
                        "position_y": self._PYVISTA_VERTICAL_SCALAR_BAR_POSITION_Y,
                        "vertical": True,
                    }
                )
            return args

        args = dict(self._SCALAR_BAR_ARGS)
        args["title"] = ""
        args["color"] = self._overlay_label_color()
        if self._scalar_bar_orientation == self._SCALAR_BAR_HORIZONTAL:
            args.update(
                {
                    "width": self._HORIZONTAL_SCALAR_BAR_WIDTH,
                    "height": self._HORIZONTAL_SCALAR_BAR_HEIGHT,
                    "position_x": self._HORIZONTAL_SCALAR_BAR_POSITION_X,
                    "position_y": self._HORIZONTAL_SCALAR_BAR_POSITION_Y,
                    "vertical": False,
                }
            )
        else:
            args.update(
                {
                    "width": self._VERTICAL_SCALAR_BAR_WIDTH,
                    "height": self._VERTICAL_SCALAR_BAR_HEIGHT,
                    "position_x": self._VERTICAL_SCALAR_BAR_POSITION_X,
                    "position_y": self._VERTICAL_SCALAR_BAR_POSITION_Y,
                    "vertical": True,
                }
            )
        return args

    def _add_view_overlay(self) -> None:
        if not self._overlay_text:
            return

        lines = self._overlay_text.splitlines()
        if not lines:
            return

        self.plotter.add_text(
            lines[0],
            position=(0.012, 0.962),
            viewport=True,
            font_size=13,
            color=self._overlay_primary_color(),
            name="result_view_overlay",
            render=False,
        )
        if len(lines) > 1:
            self.plotter.add_text(
                lines[1],
                position=(0.012, 0.920),
                viewport=True,
                font_size=13,
                color=self._overlay_primary_color(),
                name="result_display_overlay",
                render=False,
            )

    def _add_scalar_bar_title_overlay(
        self,
        result_field: ModelResultField | HorizontalPressureIntensityField | None,
    ) -> None:
        if (
            result_field is None
            or self._display_component == "Wireframe"
            or self._scalar_bar_orientation != self._SCALAR_BAR_HORIZONTAL
        ):
            return

        self.plotter.add_text(
            self._scalar_title(result_field),
            position=(
                self._HORIZONTAL_SCALAR_BAR_POSITION_X + self._HORIZONTAL_SCALAR_BAR_WIDTH * 0.43,
                self._HORIZONTAL_SCALAR_BAR_POSITION_Y + self._HORIZONTAL_SCALAR_BAR_HEIGHT,
            ),
            viewport=True,
            font_size=11,
            color=self._overlay_label_color(),
            name="scalar_bar_title_overlay",
            render=False,
        )

    def _add_scalar_bar_overlays(
        self,
        result_field: ModelResultField | HorizontalPressureIntensityField | None,
        clim: tuple[float, float] | None,
    ) -> None:
        if result_field is None or self._display_component == "Wireframe" or clim is None:
            return
        if self._scalar_bar_style == self._SCALAR_BAR_STYLE_PYVISTA:
            self._add_pyvista_scalar_bar_overlays(result_field)
            return

        scalar_bar = self.plotter.scalar_bars[""] if "" in self.plotter.scalar_bars.keys() else None
        target_intervals = 5 if self._scalar_bar_orientation == self._SCALAR_BAR_HORIZONTAL else 7
        tick_values, tick_step = self._scalar_bar_ticks(clim, target_intervals=target_intervals)
        scale_factor, scale_text = self._scalar_display_scale(clim)
        if scalar_bar is not None:
            scalar_bar.SetTitle("")
            scalar_bar.SetDrawTickLabels(False)
            scalar_bar.SetDrawAnnotations(False)
            scalar_bar.SetUseCustomLabels(False)
            scalar_bar.SetNumberOfLabels(0)
            scalar_bar.SetMaximumNumberOfColors(max(10, len(tick_values) - 1))

        if self._scalar_bar_orientation == self._SCALAR_BAR_HORIZONTAL:
            self._add_horizontal_scalar_bar_overlays(result_field, clim, tick_values, tick_step, scale_factor, scale_text)
        else:
            self._add_vertical_scalar_bar_overlays(result_field, clim, tick_values, tick_step, scale_factor, scale_text)

    def _add_pyvista_scalar_bar_overlays(
        self,
        result_field: ModelResultField | HorizontalPressureIntensityField,
    ) -> None:
        scalar_bar = self.plotter.scalar_bars[""] if "" in self.plotter.scalar_bars.keys() else None
        if scalar_bar is not None:
            scalar_bar.SetTitle("")
            scalar_bar.SetLabelFormat("%.3g")
            if self._scalar_bar_orientation == self._SCALAR_BAR_HORIZONTAL:
                scalar_bar.SetTextPositionToPrecedeScalarBar()
                scalar_bar.SetTextPad(self._PYVISTA_HORIZONTAL_LABEL_TEXT_PAD)
            else:
                scalar_bar.SetTextPositionToSucceedScalarBar()
                scalar_bar.SetTextPad(self._PYVISTA_VERTICAL_LABEL_TEXT_PAD)

        title_text = self._scalar_title(result_field)
        if self._scalar_bar_orientation == self._SCALAR_BAR_HORIZONTAL:
            title_x = self._PYVISTA_HORIZONTAL_TITLE_POSITION_X
            title_y = self._PYVISTA_HORIZONTAL_TITLE_POSITION_Y
        else:
            title_y = self._PYVISTA_VERTICAL_TITLE_POSITION_Y
            title_x = self._clamped_overlay_text_x(
                self._PYVISTA_VERTICAL_TITLE_POSITION_X,
                title_text,
                font_size=self._PYVISTA_SCALAR_BAR_TITLE_FONT_SIZE,
                right_margin=0.985,
            )
        self.plotter.add_text(
            title_text,
            position=(title_x, title_y),
            viewport=True,
            font_size=self._PYVISTA_SCALAR_BAR_TITLE_FONT_SIZE,
            color=self._overlay_primary_color(),
            name="scalar_bar_title_overlay",
            render=False,
        )

    def _add_vertical_scalar_bar_overlays(
        self,
        result_field: ModelResultField | HorizontalPressureIntensityField,
        clim: tuple[float, float],
        tick_values: list[float],
        tick_step: float,
        scale_factor: float,
        scale_text: str,
    ) -> None:
        label_x = self._VERTICAL_SCALAR_BAR_POSITION_X + self._VERTICAL_SCALAR_BAR_WIDTH - 0.015
        title_y = self._VERTICAL_SCALAR_BAR_POSITION_Y + self._VERTICAL_SCALAR_BAR_HEIGHT + 0.025
        title_text = self._scalar_title(result_field)
        title_x = self._clamped_overlay_text_x(
            self._VERTICAL_SCALAR_BAR_POSITION_X - 0.01,
            title_text,
            font_size=11,
            right_margin=0.985,
        )
        self.plotter.add_text(
            title_text,
            position=(title_x, title_y),
            viewport=True,
            font_size=11,
            color=self._overlay_primary_color(),
            name="scalar_bar_title_overlay",
            render=False,
        )
        if scale_text:
            self.plotter.add_text(
                scale_text,
                position=(self._VERTICAL_SCALAR_BAR_POSITION_X - 0.006, title_y - 0.028),
                viewport=True,
                font_size=10,
                color=self._overlay_muted_color(),
                name="scalar_bar_scale_overlay",
                render=False,
            )
        label_items = self._vertical_scalar_label_items(clim, tick_values, tick_step, scale_factor)
        for index, (label_text, label_y) in enumerate(label_items):
            self.plotter.add_text(
                label_text,
                position=(label_x, label_y),
                viewport=True,
                font_size=10,
                color=self._overlay_label_color(),
                name=f"scalar_bar_tick_label_{index}",
                render=False,
            )

    def _add_horizontal_scalar_bar_overlays(
        self,
        result_field: ModelResultField | HorizontalPressureIntensityField,
        clim: tuple[float, float],
        tick_values: list[float],
        tick_step: float,
        scale_factor: float,
        scale_text: str,
    ) -> None:
        title_y = self._HORIZONTAL_SCALAR_BAR_POSITION_Y + self._HORIZONTAL_SCALAR_BAR_HEIGHT - 0.04
        self.plotter.add_text(
            self._scalar_title(result_field),
            position=(self._HORIZONTAL_SCALAR_BAR_POSITION_X, title_y),
            viewport=True,
            font_size=11,
            color=self._overlay_primary_color(),
            name="scalar_bar_title_overlay",
            render=False,
        )
        if scale_text:
            self.plotter.add_text(
                scale_text,
                position=(self._HORIZONTAL_SCALAR_BAR_POSITION_X + 0.19, title_y),
                viewport=True,
                font_size=10,
                color=self._overlay_muted_color(),
                name="scalar_bar_scale_overlay",
                render=False,
            )

        label_items = self._horizontal_scalar_label_items(clim, tick_values, tick_step, scale_factor)
        for index, (label_text, label_x) in enumerate(label_items):
            self.plotter.add_text(
                label_text,
                position=(label_x, max(0.015, self._HORIZONTAL_SCALAR_BAR_POSITION_Y - 0.027)),
                viewport=True,
                font_size=10,
                color=self._overlay_label_color(),
                name=f"scalar_bar_tick_label_{index}",
                render=False,
            )

    def _horizontal_scalar_label_items(
        self,
        clim: tuple[float, float],
        tick_values: list[float],
        tick_step: float,
        scale_factor: float,
    ) -> list[tuple[str, float]]:
        values = self._scalar_bar_label_values(clim, tick_values, tick_step)
        if not values:
            return []

        viewport_width = max(float(getattr(self.plotter, "window_size", (900, 700))[0]), 1.0)
        candidates: list[dict[str, object]] = []
        for value in values:
            text = self._format_scalar_label(value, tick_step, scale_factor)
            tick_x = self._scalar_bar_tick_x(value, clim)
            width = self._estimated_text_width(text, font_size=10, viewport_width=viewport_width)
            is_minimum = math.isclose(value, clim[0], rel_tol=1e-9, abs_tol=max(abs(tick_step) * 1e-7, 1e-9))
            is_maximum = math.isclose(value, clim[1], rel_tol=1e-9, abs_tol=max(abs(tick_step) * 1e-7, 1e-9))
            if is_minimum:
                label_x = tick_x
                priority = 0
            elif is_maximum:
                label_x = tick_x - width
                priority = 0
            else:
                label_x = tick_x - width * 0.5
                priority = 1 if abs(value) <= max(abs(tick_step) * 1e-7, 1e-9) else 2

            label_x = max(0.015, min(0.985 - width, label_x))
            candidates.append(
                {
                    "value": value,
                    "text": text,
                    "x": label_x,
                    "left": label_x,
                    "right": label_x + width,
                    "priority": priority,
                }
            )

        kept: list[dict[str, object]] = []
        padding = self._estimated_text_width("00", font_size=10, viewport_width=viewport_width)
        for candidate in sorted(candidates, key=lambda item: (int(item["priority"]), abs(float(item["value"])))):
            left = float(candidate["left"])
            right = float(candidate["right"])
            if any(left < float(item["right"]) + padding and right + padding > float(item["left"]) for item in kept):
                continue
            kept.append(candidate)

        kept.sort(key=lambda item: float(item["value"]))
        return [(str(item["text"]), float(item["x"])) for item in kept]

    def _vertical_scalar_label_items(
        self,
        clim: tuple[float, float],
        tick_values: list[float],
        tick_step: float,
        scale_factor: float,
    ) -> list[tuple[str, float]]:
        values = self._scalar_bar_label_values(clim, tick_values, tick_step)
        if not values:
            return []

        candidates: list[dict[str, object]] = []
        for value in values:
            text = self._format_scalar_label(value, tick_step, scale_factor)
            y = self._scalar_bar_tick_y(value, clim)
            is_minimum = math.isclose(value, clim[0], rel_tol=1e-9, abs_tol=max(abs(tick_step) * 1e-7, 1e-9))
            is_maximum = math.isclose(value, clim[1], rel_tol=1e-9, abs_tol=max(abs(tick_step) * 1e-7, 1e-9))
            priority = 0 if is_minimum or is_maximum else 1 if abs(value) <= max(abs(tick_step) * 1e-7, 1e-9) else 2
            candidates.append({"value": value, "text": text, "y": y, "priority": priority})

        kept: list[dict[str, object]] = []
        min_gap = 0.032
        for candidate in sorted(candidates, key=lambda item: (int(item["priority"]), abs(float(item["value"])))):
            y = float(candidate["y"])
            if any(abs(y - float(item["y"])) < min_gap for item in kept):
                continue
            kept.append(candidate)

        kept.sort(key=lambda item: float(item["value"]))
        return [(str(item["text"]), float(item["y"])) for item in kept]

    def _estimated_text_width(self, text: str, font_size: int, viewport_width: float) -> float:
        return max(len(text), 1) * font_size * 0.58 / viewport_width

    def _clamped_overlay_text_x(
        self,
        preferred_x: float,
        text: str,
        font_size: int,
        right_margin: float = 0.985,
        left_margin: float = 0.015,
    ) -> float:
        viewport_width = max(float(getattr(self.plotter, "window_size", (900, 700))[0]), 1.0)
        width = self._estimated_text_width(text, font_size=font_size, viewport_width=viewport_width)
        return max(left_margin, min(preferred_x, right_margin - width))

    def _scalar_bar_label_values(
        self,
        clim: tuple[float, float],
        tick_values: list[float],
        tick_step: float,
    ) -> list[float]:
        minimum, maximum = clim
        values = [minimum]
        for value in tick_values:
            if minimum < value < maximum:
                values.append(value)
        values.append(maximum)

        deduped: list[float] = []
        tolerance = max(abs(tick_step) * 1e-6, 1e-9)
        for value in values:
            if not deduped or abs(value - deduped[-1]) > tolerance:
                deduped.append(value)
        return deduped

    def _scalar_bar_ticks(self, clim: tuple[float, float], target_intervals: int = 7) -> tuple[list[float], float]:
        minimum, maximum = clim
        if maximum <= minimum:
            return [minimum], 1.0

        step = self._choose_tick_step(minimum, maximum, target_intervals=target_intervals)
        if step <= 0.0:
            return [minimum, maximum], maximum - minimum

        start = math.ceil(minimum / step) * step
        end = math.floor(maximum / step) * step
        tick_values: list[float] = []
        if start <= end:
            count = int(round((end - start) / step)) + 1
            for index in range(count):
                tick_values.append(start + index * step)

        if minimum < 0.0 < maximum and not any(abs(value) <= step * 1e-6 for value in tick_values):
            tick_values.append(0.0)

        if not tick_values:
            tick_values = [minimum, maximum]

        rounded: list[float] = []
        for value in sorted(tick_values):
            normalized = 0.0 if abs(value) <= step * 1e-6 else float(value)
            if rounded and abs(normalized - rounded[-1]) < max(step * 1e-6, 1e-9):
                continue
            rounded.append(normalized)
        return rounded, step

    def _choose_tick_step(
        self,
        minimum: float,
        maximum: float,
        target_intervals: int,
    ) -> float:
        data_range = maximum - minimum
        if data_range <= 0.0:
            return 1.0

        raw_step = data_range / max(target_intervals, 1)
        base_power = math.floor(math.log10(raw_step))
        multipliers = (1.0, 1.5, 2.0, 2.5, 5.0, 7.5, 10.0)
        magnitude = 10.0**base_power
        normalized = raw_step / magnitude
        for multiplier in multipliers:
            if normalized <= multiplier + 1e-12:
                return multiplier * magnitude
        return 10.0 * magnitude

    def _scalar_display_scale(self, clim: tuple[float, float]) -> tuple[float, str]:
        minimum, maximum = clim
        max_abs = max(abs(minimum), abs(maximum))
        if max_abs <= 0.0:
            return 1.0, ""

        exponent = int(math.floor(math.log10(max_abs)))
        engineering_exponent = int(math.floor(exponent / 3) * 3)
        if abs(engineering_exponent) < 3:
            return 1.0, ""

        return 10.0**engineering_exponent, f"E{engineering_exponent:+d}".replace("+", "")

    def _format_scalar_label(self, value: float, step: float, scale_factor: float) -> str:
        scaled_value = value / scale_factor
        scaled_step = abs(step / scale_factor) if scale_factor != 0.0 else abs(step)
        if abs(scaled_value) <= max(scaled_step * 1e-6, 1e-9):
            scaled_value = 0.0

        if scaled_step >= 1.0:
            if abs(scaled_step - round(scaled_step)) < 1e-9:
                decimals = 0
            elif abs(scaled_step * 10.0 - round(scaled_step * 10.0)) < 1e-8:
                decimals = 1
            else:
                decimals = 2
        else:
            decimals = min(4, max(1, int(math.ceil(-math.log10(scaled_step))) + 1))

        text = f"{scaled_value:.{decimals}f}"
        if "." in text:
            text = text.rstrip("0").rstrip(".")
        return text

    def _scalar_bar_tick_y(self, value: float, clim: tuple[float, float]) -> float:
        minimum, maximum = clim
        if maximum <= minimum:
            return self._VERTICAL_SCALAR_BAR_POSITION_Y

        ratio = (value - minimum) / (maximum - minimum)
        ratio = max(0.0, min(1.0, ratio))
        y = self._VERTICAL_SCALAR_BAR_POSITION_Y + ratio * self._VERTICAL_SCALAR_BAR_HEIGHT - 0.011
        return max(0.02, min(0.96, y))

    def _scalar_bar_tick_x(self, value: float, clim: tuple[float, float]) -> float:
        minimum, maximum = clim
        if maximum <= minimum:
            return self._HORIZONTAL_SCALAR_BAR_POSITION_X

        ratio = (value - minimum) / (maximum - minimum)
        ratio = max(0.0, min(1.0, ratio))
        x = self._HORIZONTAL_SCALAR_BAR_POSITION_X + ratio * self._HORIZONTAL_SCALAR_BAR_WIDTH
        return max(0.02, min(0.96, x))

    def apply_up_axis(self, up_axis: str, render: bool = True) -> None:
        self._up_axis = up_axis.upper()
        view_up = (0, 1, 0) if self._up_axis == "Y" else (0, 0, 1)
        self.plotter.view_vector((1, -1, 1), viewup=view_up, render=False)
        if render:
            self.plotter.render()

    def set_joint_visibility(self, visible: bool) -> None:
        self._show_joints = visible
        if self._node_actor is not None:
            self._node_actor.SetVisibility(visible)
            self.plotter.render()

    def are_joints_visible(self) -> bool:
        return self._show_joints

    def set_grid_visibility(self, visible: bool, rerender: bool = True) -> None:
        self._show_grid = visible
        if self._current_model is None:
            self.show_placeholder()
            return
        self.render_model(
            self._current_model,
            result_field=self._current_result_field,
            display_component=self._display_component,
            deformation_scale=self._deformation_scale,
            reset_camera=False,
            color_map=self._color_map,
            scalar_bar_orientation=self._scalar_bar_orientation,
            scalar_limits_override=self._scalar_limits_override,
            overlay_text=self._overlay_text,
        )
        if rerender:
            self.plotter.render()

    def set_extrude(self, enabled: bool, rerender: bool = True) -> None:
        self._show_extrude = enabled
        if self._current_model is None:
            return
        self.render_model(
            self._current_model,
            result_field=self._current_result_field,
            display_component=self._display_component,
            deformation_scale=self._deformation_scale,
            reset_camera=False,
            color_map=self._color_map,
            scalar_bar_orientation=self._scalar_bar_orientation,
            scalar_limits_override=self._scalar_limits_override,
            overlay_text=self._overlay_text,
        )
        if rerender:
            self.plotter.render()

    def set_beam_color_mode(self, mode: str, rerender: bool = True) -> None:
        self._beam_color_mode = mode
        if self._current_model is None:
            return
        self.render_model(
            self._current_model,
            result_field=self._current_result_field,
            display_component=self._display_component,
            deformation_scale=self._deformation_scale,
            reset_camera=False,
            color_map=self._color_map,
            scalar_bar_orientation=self._scalar_bar_orientation,
            scalar_limits_override=self._scalar_limits_override,
            overlay_text=self._overlay_text,
        )
        if rerender:
            self.plotter.render()

    def set_background_mode(self, mode: str, rerender: bool = True) -> None:
        self._background_mode = "White" if str(mode).lower().startswith("w") else "Dark"
        if self._current_model is None:
            self.show_placeholder()
            return
        self.render_model(
            self._current_model,
            result_field=self._current_result_field,
            display_component=self._display_component,
            deformation_scale=self._deformation_scale,
            reset_camera=False,
            color_map=self._color_map,
            scalar_bar_orientation=self._scalar_bar_orientation,
            scalar_limits_override=self._scalar_limits_override,
            overlay_text=self._overlay_text,
        )
        if rerender:
            self.plotter.render()

    def set_isolate_selected_beam(self, enabled: bool, rerender: bool = True) -> None:
        self._isolate_selected_beam = enabled
        if self._current_model is None:
            return
        self.render_model(
            self._current_model,
            result_field=self._current_result_field,
            display_component=self._display_component,
            deformation_scale=self._deformation_scale,
            reset_camera=False,
            color_map=self._color_map,
            scalar_bar_orientation=self._scalar_bar_orientation,
            scalar_limits_override=self._scalar_limits_override,
            overlay_text=self._overlay_text,
        )
        if rerender:
            self.plotter.render()

    def is_grid_visible(self) -> bool:
        return self._show_grid

    def clear_selection(self, render: bool = True) -> None:
        self._selected_beam_id = None
        self._selected_joint_id = None
        if self._beam_highlight_actor is not None:
            self._beam_highlight_actor.SetVisibility(False)
        if self._joint_highlight_actor is not None:
            self._joint_highlight_actor.SetVisibility(False)
        if self._on_beam_selected is not None:
            self._on_beam_selected(None)
        if self._on_joint_selected is not None:
            self._on_joint_selected(None)
        if render:
            self.plotter.render()

    def _ensure_highlight_actors(self) -> None:
        if self._joint_highlight_actor is None and self._joint_highlight_mesh.n_points > 0:
            self._joint_highlight_actor = self.plotter.add_mesh(
                self._joint_highlight_mesh,
                color="#2ecc71",
                point_size=16,
                render_points_as_spheres=True,
                pickable=False,
                render=False,
            )
            self._joint_highlight_actor.SetVisibility(False)

    def _handle_pick_point(self, picked_point) -> None:
        if picked_point is None or self._current_model is None:
            return

        picked = np.asarray(picked_point, dtype=float)
        if picked.shape != (3,) or np.isnan(picked).any():
            return

        node = self._find_nearest_joint(picked) if self._show_joints else None
        beam = self._find_nearest_beam(picked)
        beam_distance = self._distance_to_beam(picked, beam) if beam is not None else float("inf")
        joint_distance = self._distance_to_joint(picked, node) if node is not None else float("inf")
        beam_tolerance = self._beam_pick_tolerance()
        joint_tolerance = self._joint_pick_tolerance()

        beam_in_range = beam is not None and beam_distance <= beam_tolerance
        joint_in_range = node is not None and joint_distance <= joint_tolerance

        if not beam_in_range and not joint_in_range:
            return

        if beam_in_range:
            self._highlight_beam(beam)
            return

        if joint_in_range:
            self._highlight_joint(node)

    def _beam_pick_tolerance(self) -> float:
        return max(self._model_diagonal() * 0.025, 1.5)

    def _joint_pick_tolerance(self) -> float:
        return max(self._model_diagonal() * 0.006, 0.35)

    def _model_diagonal(self) -> float:
        if len(self._current_points) == 0:
            return 1.0
        mins = self._current_points.min(axis=0)
        maxs = self._current_points.max(axis=0)
        diagonal = float(np.linalg.norm(maxs - mins))
        return diagonal if diagonal > 0 else 1.0

    def _find_nearest_joint(self, point: np.ndarray) -> NodeData | None:
        if len(self._current_points) == 0 or self._current_model is None:
            return None

        distances = np.sum((self._current_points - point) ** 2, axis=1)
        nearest_index = int(np.argmin(distances))
        node_id = self._current_node_ids[nearest_index]
        return self._current_model.nodes.get(node_id)

    def _find_nearest_beam(self, point: np.ndarray) -> BeamData | None:
        if len(self._beam_segments) == 0:
            return None

        start_points = self._beam_segments[:, 0, :]
        end_points = self._beam_segments[:, 1, :]
        ab = end_points - start_points
        ap = point - start_points
        denom = np.sum(ab * ab, axis=1)
        denom[denom == 0] = 1.0
        t = np.clip(np.sum(ap * ab, axis=1) / denom, 0.0, 1.0)
        closest = start_points + ab * t[:, None]
        distances = np.sum((closest - point) ** 2, axis=1)
        beam_index = int(np.argmin(distances))
        return self._beam_lookup[beam_index]

    def _distance_to_joint(self, point: np.ndarray, node: NodeData) -> float:
        joint_point = np.array([node.x, node.y, node.z], dtype=float)
        return float(np.linalg.norm(joint_point - point))

    def _distance_to_beam(self, point: np.ndarray, beam: BeamData) -> float:
        if self._current_model is None:
            return float("inf")

        start = self._current_model.nodes[beam.start_node_id]
        end = self._current_model.nodes[beam.end_node_id]
        a = np.array([start.x, start.y, start.z], dtype=float)
        b = np.array([end.x, end.y, end.z], dtype=float)
        ab = b - a
        denom = float(np.dot(ab, ab))
        if denom == 0:
            return float(np.linalg.norm(point - a))
        t = float(np.clip(np.dot(point - a, ab) / denom, 0.0, 1.0))
        closest = a + ab * t
        return float(np.linalg.norm(point - closest))

    def _highlight_beam(self, beam: BeamData, render: bool = True, notify: bool = True) -> None:
        if self._current_model is None:
            return

        self._selected_beam_id = beam.beam_id
        self._selected_joint_id = None
        start_index = self._node_index_by_id.get(beam.start_node_id)
        end_index = self._node_index_by_id.get(beam.end_node_id)
        if start_index is None or end_index is None:
            return

        if self._beam_highlight_actor is None:
            pass
        else:
            self.plotter.remove_actor(self._beam_highlight_actor, render=False)
            self._beam_highlight_actor = None

        if self._show_extrude and self._beam_mesh.n_cells > 0 and "beam_id" in self._beam_mesh.cell_data:
            matches = np.flatnonzero(self._beam_mesh.cell_data["beam_id"] == beam.beam_id)
            if len(matches) > 0:
                self._beam_highlight_mesh = (
                    self._beam_mesh.extract_cells(matches)
                    .extract_surface(algorithm="dataset_surface")
                    .clean()
                )
                self._beam_highlight_actor = self.plotter.add_mesh(
                    self._beam_highlight_mesh,
                    color="#ffd23f",
                    opacity=0.65,
                    show_edges=True,
                    line_width=2.0,
                    pickable=False,
                    render=False,
                )
        if self._beam_highlight_actor is None:
            line_points = np.array(
                [self._current_points[start_index], self._current_points[end_index]],
                dtype=float,
            )
            self._beam_highlight_mesh = pv.PolyData()
            self._beam_highlight_mesh.points = line_points
            self._beam_highlight_mesh.lines = np.array([2, 0, 1], dtype=np.int32)
            self._beam_highlight_actor = self.plotter.add_mesh(
                self._beam_highlight_mesh,
                color="#ffd23f",
                line_width=5,
                render_lines_as_tubes=False,
                pickable=False,
                render=False,
            )
        self._beam_highlight_actor.SetVisibility(True)

        if self._joint_highlight_actor is not None:
            self._joint_highlight_actor.SetVisibility(False)
        if notify and self._on_beam_selected is not None:
            self._on_beam_selected(beam)
        if notify and self._on_joint_selected is not None:
            self._on_joint_selected(None)
        if notify and self._isolate_selected_beam and self._current_model is not None:
            self.render_model(
                self._current_model,
                result_field=self._current_result_field,
                display_component=self._display_component,
                deformation_scale=self._deformation_scale,
                reset_camera=False,
                color_map=self._color_map,
                scalar_bar_orientation=self._scalar_bar_orientation,
                scalar_limits_override=self._scalar_limits_override,
                overlay_text=self._overlay_text,
            )
            return
        if render:
            self.plotter.render()

    def _highlight_joint(self, node: NodeData, render: bool = True, notify: bool = True) -> None:
        self._selected_joint_id = node.node_id
        self._selected_beam_id = None
        node_index = self._node_index_by_id.get(node.node_id)
        if node_index is None:
            return

        self._joint_highlight_mesh.points = np.array([self._current_points[node_index]], dtype=float)
        self._joint_highlight_mesh.Modified()
        if self._joint_highlight_actor is None:
            self._joint_highlight_actor = self.plotter.add_mesh(
                self._joint_highlight_mesh,
                color="#2ecc71",
                point_size=16,
                render_points_as_spheres=True,
                pickable=False,
                render=False,
            )
        self._joint_highlight_actor.SetVisibility(True)

        if self._beam_highlight_actor is not None:
            self._beam_highlight_actor.SetVisibility(False)
        if notify and self._on_joint_selected is not None:
            self._on_joint_selected(node)
        if notify and self._on_beam_selected is not None:
            self._on_beam_selected(None)
        if render:
            self.plotter.render()
