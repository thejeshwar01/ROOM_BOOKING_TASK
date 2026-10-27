import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api.js";
import { useApi } from "../useApi.js";
import { equipmentList, roomImages } from "../data.js";
import ErrorMessage from "../components/ErrorMessage.jsx";

// ///ROOMS PAGE///
export default function Rooms() {
    const [capacity, setCapacity] = useState("");
    const [equipment, setEquipment] = useState("");
    const [floor, setFloor] = useState("");
    const [selectedId, setSelectedId] = useState(null);

    useEffect(() => { document.title = "MEETING ROOMS"; }, []);

    // Filtering happens in the back end
    const rooms = useApi(() => api.getRooms({ minCapacity: capacity, equipment, floor }), [capacity, equipment, floor]);
    const floors = useApi(() => api.getFloors(), []);

    function clearFilters() {
        setCapacity("");
        setEquipment("");
        setFloor("");
    }

    const visibleRooms = rooms.data ?? [];

    return (
        <section id="rooms" className="p-lg-4 p-3">
            <div className="container-fluid">
                <div className="row g-4">

                    {/* Room list */}
                    <div className="col-12">
                        <h1 className="page-title">Meeting Rooms</h1>
                        <p className="page-subtitle">Find and book the perfect meeting room for your team.</p>

                        {/* Filters */}
                        <div className="row g-3 mb-4">
                            <div className="col-md-3 col-6">
                                <div className="filter-box">
                                    <i className="bi bi-people"></i>
                                    <div className="flex-grow-1">
                                        <label htmlFor="filter-capacity">Min Capacity</label>
                                        <select className="form-select" id="filter-capacity" value={capacity} onChange={(e) => setCapacity(e.target.value)}>
                                            <option value="">Any</option>
                                            <option value="4">4+</option>
                                            <option value="8">8+</option>
                                            <option value="12">12+</option>
                                            <option value="20">20+</option>
                                        </select>
                                    </div>
                                </div>
                            </div>
                            <div className="col-md-3 col-6">
                                <div className="filter-box">
                                    <i className="bi bi-display"></i>
                                    <div className="flex-grow-1">
                                        <label htmlFor="filter-equipment">Equipment</label>
                                        <select className="form-select" id="filter-equipment" value={equipment} onChange={(e) => setEquipment(e.target.value)}>
                                            <option value="">Any</option>
                                            {Object.entries(equipmentList).map(([key, item]) => (
                                                <option key={key} value={key}>{item.label}</option>
                                            ))}
                                        </select>
                                    </div>
                                </div>
                            </div>
                            <div className="col-md-3 col-6">
                                <div className="filter-box">
                                    <i className="bi bi-building"></i>
                                    <div className="flex-grow-1">
                                        <label htmlFor="filter-floor">Floor</label>
                                        <select className="form-select" id="filter-floor" value={floor} onChange={(e) => setFloor(e.target.value)}>
                                            <option value="">Any</option>
                                            {(floors.data ?? []).map((name) => <option key={name} value={name}>{name}</option>)}
                                        </select>
                                    </div>
                                </div>
                            </div>
                            <div className="col-md-3 col-6">
                                <button className="btn btn-outline-gold w-100 h-100" id="clear-filters" onClick={clearFilters}>Clear Filters</button>
                            </div>
                        </div>

                        <ErrorMessage error={rooms.error} className="mb-3" />

                        {/* Room cards */}
                        <div className="row g-5" id="room-list">
                            {visibleRooms.map((room) => (
                                <div className="col-lg-4 col-md-6 col-12 room-col" key={room.id}>
                                    <RoomCard room={room} selected={room.id === selectedId} onSelect={() => setSelectedId(room.id)} />
                                </div>
                            ))}
                        </div>
                        {rooms.loading && !rooms.data && <p className="empty-message text-center">Loading rooms...</p>}
                        {rooms.data && visibleRooms.length === 0 && (
                            <p className="empty-message text-center" id="no-rooms">No rooms match the selected filters.</p>
                        )}
                    </div>

                </div>
            </div>
        </section>
    );
}

function RoomCard({ room, selected, onSelect }) {
    const inactive = !room.isActive;

    // First click selects the room, clicking "Book Room" on a selected room opens reservations
    function handleActionClick(e) {
        if (!selected || inactive) e.preventDefault();
    }

    return (
        <div className={"card room-card" + (selected ? " selected" : "")} tabIndex="0"
             onClick={onSelect}
             onKeyDown={(e) => { if (e.key === "Enter" && e.target === e.currentTarget) onSelect(); }}>
            <img src={roomImages[room.code]} className="card-img-top" alt={"Room " + room.name} />
            <div className="card-body">
                <div className="d-flex justify-content-between align-items-start">
                    <div>
                        <h5 className="card-title">{room.name}</h5>
                        <p className="room-id">{room.code}</p>
                    </div>
                    <span className={"status-badge " + (inactive ? "status-inactive" : "status-active")}>{inactive ? "Inactive" : "Active"}</span>
                </div>
                <div className="room-meta">
                    <span title="Capacity"><i className="bi bi-people"></i> {room.capacity}</span>
                    <span title="Floor"><i className="bi bi-building"></i> {room.floor}</span>
                </div>
                {/* Small equipment icons */}
                <div className="room-equipment">
                    {room.equipment.map((key) => (
                        <i key={key} className={"bi " + equipmentList[key].icon} title={equipmentList[key].label}></i>
                    ))}
                </div>
            </div>

            {/* Panel revealed when the card is hovered or selected */}
            <div className="room-hover">
                <p className="room-description">{room.description}</p>
                {/* Inactive rooms cannot be booked */}
                <Link className={"btn btn-gold room-action" + (inactive ? " disabled" : "")} aria-disabled={inactive}
                      to={"/reservations?room=" + room.id} onClick={handleActionClick}>
                    {inactive ? <><i className="bi bi-slash-circle"></i> Unavailable</>
                        : selected ? <><i className="bi bi-calendar2-plus"></i> Book Room</>
                        : <><i className="bi bi-check2-circle"></i> Select Room</>}
                </Link>
            </div>
        </div>
    );
}
