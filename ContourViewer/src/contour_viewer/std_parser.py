from __future__ import annotations

import re
from pathlib import Path

from .models import BeamData, LoadCaseInfo, ModelGeometry, NodeData


NODE_PATTERN = re.compile(
    r"^\s*(\d+)\s+([-+]?\d*\.?\d+(?:[Ee][-+]?\d+)?)\s+([-+]?\d*\.?\d+(?:[Ee][-+]?\d+)?)\s+([-+]?\d*\.?\d+(?:[Ee][-+]?\d+)?)\s*$"
)
BEAM_PATTERN = re.compile(r"^\s*(\d+)\s+(\d+)\s+(\d+)\s*$")
UNIT_PATTERN = re.compile(r"^\s*UNIT\s+([A-Z]+)", re.IGNORECASE)
SET_Z_UP_PATTERN = re.compile(r"^\s*SET\s+Z\s+UP\s*$", re.IGNORECASE)
SET_Y_UP_PATTERN = re.compile(r"^\s*SET\s+Y\s+UP\s*$", re.IGNORECASE)
PRIMARY_LOAD_PATTERN = re.compile(r"^\s*LOAD\s+(\d+)(?:\s+(.*?))?\s*$", re.IGNORECASE)
COMBINATION_LOAD_PATTERN = re.compile(r"^\s*LOAD\s+COMB(?:INATION)?\s+(\d+)(?:\s+(.*?))?\s*$", re.IGNORECASE)
FIXED_SUPPORT_PATTERN = re.compile(r"^(.*?)\bFIXED\b(?:\s+BUT.*)?$", re.IGNORECASE)
PINNED_SUPPORT_PATTERN = re.compile(r"^(.*?)\bPINNED\b$", re.IGNORECASE)

SECTION_STOP_PREFIXES = (
    "START ",
    "END ",
    "DEFINE ",
    "CONSTANTS",
    "SUPPORTS",
    "LOAD ",
    "REPEAT LOAD",
    "MEMBER RELEASE",
    "MEMBER PROPERTY",
    "ELEMENT PROPERTY",
    "FINISH",
)


def parse_std_geometry(model_path: Path) -> ModelGeometry:
    text = model_path.read_text(encoding="utf-8", errors="ignore")
    lines = text.splitlines()

    unit_length = _find_first_unit(lines)
    up_axis = _find_up_axis(lines)
    node_section = _collect_section(lines, "JOINT COORDINATES")
    beam_section = _collect_section(lines, "MEMBER INCIDENCES")
    support_section = _collect_section(lines, "SUPPORTS")

    model = ModelGeometry(model_path=model_path, unit_length=unit_length, up_axis=up_axis)
    for node in _parse_node_entries(node_section):
        model.nodes[node.node_id] = node

    for beam in _parse_beam_entries(beam_section):
        if beam.start_node_id in model.nodes and beam.end_node_id in model.nodes:
            model.beams.append(beam)

    model.load_cases.extend(_parse_load_cases(lines))
    model.support_types.update(_parse_fixed_pinned_supports(support_section, model.nodes))

    return model


def _find_first_unit(lines: list[str]) -> str | None:
    for line in lines:
        match = UNIT_PATTERN.match(line)
        if match:
            return match.group(1).upper()
    return None


def _find_up_axis(lines: list[str]) -> str:
    for line in lines:
        if SET_Z_UP_PATTERN.match(line):
            return "Z"
        if SET_Y_UP_PATTERN.match(line):
            return "Y"
    return "Y"


def _collect_section(lines: list[str], section_name: str) -> str:
    collecting = False
    collected: list[str] = []

    for raw_line in lines:
        line = raw_line.rstrip()
        upper = line.strip().upper()

        if not collecting:
            if upper == section_name:
                collecting = True
            continue

        if not upper:
            continue

        if any(upper.startswith(prefix) for prefix in SECTION_STOP_PREFIXES):
            break

        collected.append(line)

    return " ".join(collected)


def _parse_node_entries(section_text: str) -> list[NodeData]:
    nodes: list[NodeData] = []
    for token in _split_records(section_text):
        match = NODE_PATTERN.match(token)
        if not match:
            continue

        node_id, x, y, z = match.groups()
        nodes.append(NodeData(int(node_id), float(x), float(y), float(z)))

    return nodes


def _parse_beam_entries(section_text: str) -> list[BeamData]:
    beams: list[BeamData] = []
    for token in _split_records(section_text):
        match = BEAM_PATTERN.match(token)
        if not match:
            continue

        beam_id, start_node, end_node = match.groups()
        beams.append(BeamData(int(beam_id), int(start_node), int(end_node)))

    return beams


def _split_records(section_text: str) -> list[str]:
    return [record.strip() for record in section_text.split(";") if record.strip()]


def _parse_load_cases(lines: list[str]) -> list[LoadCaseInfo]:
    load_cases: list[LoadCaseInfo] = []
    seen_case_ids: set[int] = set()

    for raw_line in lines:
        line = raw_line.strip()
        if not line:
            continue

        comb_match = COMBINATION_LOAD_PATTERN.match(line)
        if comb_match:
            case_id = int(comb_match.group(1))
            if case_id not in seen_case_ids:
                seen_case_ids.add(case_id)
                load_cases.append(
                    LoadCaseInfo(case_id=case_id, title=(comb_match.group(2) or "").strip(), case_type="Combo")
                )
            continue

        primary_match = PRIMARY_LOAD_PATTERN.match(line)
        if primary_match:
            case_id = int(primary_match.group(1))
            if case_id not in seen_case_ids:
                seen_case_ids.add(case_id)
                load_cases.append(
                    LoadCaseInfo(case_id=case_id, title=(primary_match.group(2) or "").strip(), case_type="Primary")
                )

    return load_cases


def _parse_fixed_pinned_supports(section_text: str, nodes: dict[int, NodeData]) -> dict[int, str]:
    supports: dict[int, str] = {}
    for record in _split_records(section_text):
        normalized = " ".join(record.replace(",", " ").split())
        fixed_match = FIXED_SUPPORT_PATTERN.match(normalized)
        pinned_match = PINNED_SUPPORT_PATTERN.match(normalized)

        support_type = None
        node_spec = None
        if fixed_match:
            support_type = "FIXED"
            node_spec = fixed_match.group(1)
        elif pinned_match:
            support_type = "PINNED"
            node_spec = pinned_match.group(1)

        if not support_type or not node_spec:
            continue

        for node_id in _expand_node_spec(node_spec):
            if node_id in nodes:
                supports[node_id] = support_type

    return supports


def _expand_node_spec(node_spec: str) -> list[int]:
    tokens = [token for token in node_spec.strip().split() if token]
    values: list[int] = []
    index = 0
    while index < len(tokens):
        token = tokens[index].upper()
        if token == "TO":
            index += 1
            continue

        if not tokens[index].lstrip("+-").isdigit():
            index += 1
            continue

        start_value = int(tokens[index])
        if index + 2 < len(tokens) and tokens[index + 1].upper() == "TO" and tokens[index + 2].lstrip("+-").isdigit():
            end_value = int(tokens[index + 2])
            step = 1 if end_value >= start_value else -1
            values.extend(range(start_value, end_value + step, step))
            index += 3
        else:
            values.append(start_value)
            index += 1

    return values
