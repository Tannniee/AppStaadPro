from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path


@dataclass(slots=True)
class NodeData:
    node_id: int
    x: float
    y: float
    z: float


@dataclass(slots=True)
class BeamData:
    beam_id: int
    start_node_id: int
    end_node_id: int
    depth: float | None = None
    width: float | None = None
    web_thickness: float | None = None
    flange_thickness_top: float | None = None
    flange_thickness_bottom: float | None = None
    beta_angle: float = 0.0
    section_name: str | None = None
    section_display_name: str | None = None
    section_property_type: int | None = None
    section_property_values: tuple[float, ...] = ()


@dataclass(slots=True)
class LoadCaseInfo:
    case_id: int
    title: str
    case_type: str = "Primary"

    @property
    def display_name(self) -> str:
        label = self.title.strip()
        suffix = f" - {label}" if label else ""
        return f"{self.case_type} {self.case_id}{suffix}"


@dataclass(slots=True)
class ModelResultField:
    case_id: int
    component: str
    unit: str | None
    display_to_model_scale: float = 1.0
    node_values: dict[int, float] = field(default_factory=dict)
    beam_values: dict[int, float] = field(default_factory=dict)
    beam_peak_positions: dict[int, float] = field(default_factory=dict)
    excluded_beam_ids: set[int] = field(default_factory=set)

    @property
    def min_value(self) -> float | None:
        values = [*self.node_values.values(), *self.beam_values.values()]
        return min(values) if values else None

    @property
    def max_value(self) -> float | None:
        values = [*self.node_values.values(), *self.beam_values.values()]
        return max(values) if values else None


@dataclass(slots=True)
class HorizontalPressureIntensityField:
    case_id: int
    component: str
    unit: str | None
    up_axis: str
    elevation: float
    node_values: dict[int, float] = field(default_factory=dict)
    reaction_values: dict[int, float] = field(default_factory=dict)
    tributary_areas: dict[int, float] = field(default_factory=dict)
    boundary: tuple[float, float, float, float] | None = None
    zone_mode: str = "Full Plane"
    zone_radius: float = 0.0
    warning: str | None = None

    @property
    def min_value(self) -> float | None:
        values = list(self.node_values.values())
        return min(values) if values else None

    @property
    def max_value(self) -> float | None:
        values = list(self.node_values.values())
        return max(values) if values else None


@dataclass(slots=True)
class ModelGeometry:
    model_path: Path
    unit_length: str | None
    up_axis: str = "Y"
    nodes: dict[int, NodeData] = field(default_factory=dict)
    beams: list[BeamData] = field(default_factory=list)
    load_cases: list[LoadCaseInfo] = field(default_factory=list)
    support_types: dict[int, str] = field(default_factory=dict)
    section_metadata_loaded: bool = False

    @property
    def node_count(self) -> int:
        return len(self.nodes)

    @property
    def beam_count(self) -> int:
        return len(self.beams)

    @property
    def support_count(self) -> int:
        return len(self.support_types)

    def support_node_ids_for_component(self, component: str) -> list[int]:
        component = component.upper()
        if component not in {"DX", "DY", "DZ"}:
            return []
        return sorted(self.support_types)
