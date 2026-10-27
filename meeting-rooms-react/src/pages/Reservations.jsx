import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Modal } from "bootstrap";
import { api } from "../api.js";
import { useApi } from "../useApi.js";
import { statusClass } from "../data.js";
import { endOfDay, formatDate, formatTime, fromDateTimeInput, nextFullHour, startOfDay, toDateTimeInput } from "../dates.js";
import ErrorMessage from "../components/ErrorMessage.jsx";

function emptyForm(roomId, organizerId) {
    const start = nextFullHour();
    const end = new Date(start.getTime() + 60 * 60 * 1000);
    return {
        id: null,
        roomId: roomId ? String(roomId) : "",
        organizerId: organizerId ? String(organizerId) : "",
        title: "",
        start: toDateTimeInput(start),
        end: toDateTimeInput(end),
        attendees: "4"
    };
}

function formFromReservation(r) {
    return {
        id: r.id,
        roomId: String(r.roomId),
        organizerId: String(r.organizerId),
        title: r.title,
        start: toDateTimeInput(r.startTime),
        end: toDateTimeInput(r.endTime),
        attendees: String(r.attendees)
    };
}

// ///RESERVATIONS PAGE///
export default function Reservations() {
    const [roomFilter, setRoomFilter] = useState("");
    const [organizerFilter, setOrganizerFilter] = useState("");
    const [statusFilter, setStatusFilter] = useState("");
    const [fromFilter, setFromFilter] = useState("");
    const [toFilter, setToFilter] = useState("");
    const [form, setForm] = useState(() => emptyForm());
    const [formError, setFormError] = useState(null);
    const [saving, setSaving] = useState(false);
    const [actionError, setActionError] = useState(null);
    const [searchParams, setSearchParams] = useSearchParams();
    const modalRef = useRef(null);

    useEffect(() => { document.title = "RESERVATIONS"; }, []);

    // Filtering happens in the back end
    const reservations = useApi(() => api.getReservations({
        roomId: roomFilter,
        organizerId: organizerFilter,
        status: statusFilter,
        from: fromFilter && startOfDay(fromFilter),
        to: toFilter && endOfDay(toFilter)
    }), [roomFilter, organizerFilter, statusFilter, fromFilter, toFilter]);

    const rooms = useApi(() => api.getRooms(), []);
    const users = useApi(() => api.getUsers(), []);
    const rules = useApi(() => api.getBookingRules(), []);

    const allRooms = rooms.data ?? [];
    const bookableRooms = allRooms.filter((room) => room.isActive);
    const allUsers = users.data ?? [];
    const selectedRoom = allRooms.find((room) => String(room.id) === form.roomId);

    // Leaving the page with the modal open must not leave the dark backdrop behind
    useEffect(() => {
        const modal = Modal.getOrCreateInstance(modalRef.current);
        return () => {
            modal.dispose();
            document.querySelectorAll(".modal-backdrop").forEach((backdrop) => backdrop.remove());
            document.body.classList.remove("modal-open");
            document.body.style.removeProperty("overflow");
            document.body.style.removeProperty("padding-right");
        };
    }, []);

    // Opened from "Book Room" on the rooms page
    const bookedRoomId = searchParams.get("room");
    useEffect(() => {
        if (!bookedRoomId || !rooms.data || !users.data) return;
        openNewForm(bookedRoomId);
        setSearchParams({}, { replace: true });
    }, [bookedRoomId, rooms.data, users.data]);

    function openNewForm(roomId) {
        setForm(emptyForm(roomId ?? bookableRooms[0]?.id, allUsers[0]?.id));
        setFormError(null);
        Modal.getOrCreateInstance(modalRef.current).show();
    }

    function openEditForm(reservation) {
        setForm(formFromReservation(reservation));
        setFormError(null);
        Modal.getOrCreateInstance(modalRef.current).show();
    }

    function updateForm(field, value) {
        setForm({ ...form, [field]: value });
    }

    function clearFilters() {
        setRoomFilter("");
        setOrganizerFilter("");
        setStatusFilter("");
        setFromFilter("");
        setToFilter("");
    }

    // The back end checks every booking rule and answers with an error code and message if one is broken
    async function handleSubmit(e) {
        e.preventDefault();
        setSaving(true);
        setFormError(null);

        const reservation = {
            roomId: Number(form.roomId),
            organizerId: Number(form.organizerId),
            title: form.title,
            startTime: fromDateTimeInput(form.start),
            endTime: fromDateTimeInput(form.end),
            attendees: Number(form.attendees)
        };

        try {
            if (form.id) await api.updateReservation(form.id, reservation);
            else await api.createReservation(reservation);

            Modal.getOrCreateInstance(modalRef.current).hide();
            reservations.reload();
        } catch (err) {
            setFormError(err);
        } finally {
            setSaving(false);
        }
    }

    async function runAction(question, action) {
        if (!confirm(question)) return;
        setActionError(null);
        try {
            await action();
            reservations.reload();
        } catch (err) {
            setActionError(err);
        }
    }

    const cancelReservation = (r) => runAction(`Cancel "${r.title}"?`, () => api.cancelReservation(r.id));
    const endEarly = (r) => runAction(`End "${r.title}" now? The room becomes free immediately.`, () => api.endReservationEarly(r.id));

    const visibleReservations = reservations.data ?? [];

    return (
        <>
            <section id="reservations" className="p-lg-4 p-3">
                <div className="container-fluid">
                    <h1 className="page-title">Reservations</h1>
                    <p className="page-subtitle">View and manage all meeting room reservations.</p>

                    {/* Filters */}
                    <div className="row g-3 mb-4">
                        <div className="col-xl-2 col-md-4 col-6">
                            <div className="filter-box">
                                <div className="flex-grow-1">
                                    <label htmlFor="filter-room">Room</label>
                                    <select className="form-select" id="filter-room" value={roomFilter} onChange={(e) => setRoomFilter(e.target.value)}>
                                        <option value="">Any</option>
                                        {allRooms.map((room) => <option key={room.id} value={room.id}>{room.name}</option>)}
                                    </select>
                                </div>
                            </div>
                        </div>
                        <div className="col-xl-2 col-md-4 col-6">
                            <div className="filter-box">
                                <div className="flex-grow-1">
                                    <label htmlFor="filter-organizer">Organizer</label>
                                    <select className="form-select" id="filter-organizer" value={organizerFilter} onChange={(e) => setOrganizerFilter(e.target.value)}>
                                        <option value="">Any</option>
                                        {allUsers.map((user) => <option key={user.id} value={user.id}>{user.fullName}</option>)}
                                    </select>
                                </div>
                            </div>
                        </div>
                        <div className="col-xl-2 col-md-4 col-6">
                            <div className="filter-box">
                                <div className="flex-grow-1">
                                    <label htmlFor="filter-status">Status</label>
                                    <select className="form-select" id="filter-status" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                                        <option value="">Any</option>
                                        {Object.keys(statusClass).map((status) => <option key={status}>{status}</option>)}
                                    </select>
                                </div>
                            </div>
                        </div>
                        <div className="col-xl-2 col-md-4 col-6">
                            <div className="filter-box">
                                <i className="bi bi-calendar3"></i>
                                <div className="flex-grow-1">
                                    <label htmlFor="filter-from">From</label>
                                    <input type="date" className="form-control" id="filter-from" value={fromFilter} max={toFilter || undefined}
                                           onChange={(e) => setFromFilter(e.target.value)} />
                                </div>
                            </div>
                        </div>
                        <div className="col-xl-2 col-md-4 col-6">
                            <div className="filter-box">
                                <i className="bi bi-calendar3"></i>
                                <div className="flex-grow-1">
                                    <label htmlFor="filter-to">To</label>
                                    <input type="date" className="form-control" id="filter-to" value={toFilter} min={fromFilter || undefined}
                                           onChange={(e) => setToFilter(e.target.value)} />
                                </div>
                            </div>
                        </div>
                        <div className="col-xl-2 col-md-4 col-12 d-flex flex-column gap-2">
                            <button className="btn btn-gold w-100 flex-grow-1" onClick={() => openNewForm()} disabled={!rooms.data || !users.data}>
                                <i className="bi bi-plus-lg"></i> New Reservation
                            </button>
                            <button className="btn btn-outline-gold btn-sm w-100" onClick={clearFilters}>Clear Filters</button>
                        </div>
                    </div>

                    <ErrorMessage error={reservations.error ?? actionError} className="mb-3" />

                    {/* Reservation table */}
                    <div className="table-panel">
                        <div className="table-responsive">
                            <table className="table align-middle mb-0" id="reservation-table">
                                <thead>
                                    <tr>
                                        <th>Room</th>
                                        <th>Organizer</th>
                                        <th>Meeting Title</th>
                                        <th>Start</th>
                                        <th>End</th>
                                        <th className="text-center">Attendees</th>
                                        <th>Status</th>
                                        <th className="text-center">Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {visibleReservations.map((r) => (
                                        <tr key={r.id}>
                                            <td><span className="room-name">{r.roomName}</span><small className="room-id d-block">{r.roomCode}</small></td>
                                            <td>{r.organizerName}</td>
                                            <td>{r.title}</td>
                                            <td>{formatDate(r.startTime)}<br />{formatTime(r.startTime)}</td>
                                            <td>
                                                {formatDate(r.endTime)}<br />{formatTime(r.endTime)}
                                                {r.isEndedEarly && <small className="ended-early d-block">Ended {formatTime(r.endedAt)}</small>}
                                            </td>
                                            <td className="text-center">{r.attendees}</td>
                                            <td>
                                                <span className={"status-badge " + statusClass[r.status]}>{r.status}</span>
                                                {r.isInProgress && <span className="status-badge status-progress ms-1">In progress</span>}
                                            </td>
                                            <td className="text-center text-nowrap">
                                                {r.canEdit && (
                                                    <button className="btn-action" title="Edit" onClick={() => openEditForm(r)}><i className="bi bi-pencil"></i></button>
                                                )}
                                                {r.canCancel && (
                                                    <button className="btn-action btn-cancel" title="Cancel" onClick={() => cancelReservation(r)}><i className="bi bi-x-circle"></i></button>
                                                )}
                                                {r.canEndEarly && (
                                                    <button className="btn-action" title="End meeting early" onClick={() => endEarly(r)}><i className="bi bi-stop-circle"></i></button>
                                                )}
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                        {reservations.loading && !reservations.data && <p className="empty-message text-center">Loading reservations...</p>}
                        {reservations.data && visibleReservations.length === 0 && (
                            <p className="empty-message text-center" id="no-reservations">No reservations match the selected filters.</p>
                        )}
                    </div>
                </div>
            </section>

            {/* ///RESERVATION MODAL/// */}
            <div className="modal fade" id="reservationModal" tabIndex="-1" aria-labelledby="reservationModalLabel" aria-hidden="true" ref={modalRef}>
                <div className="modal-dialog modal-dialog-centered">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h5 className="modal-title" id="reservationModalLabel">{form.id ? "Edit Reservation" : "New Reservation"}</h5>
                            <button type="button" className="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <form id="reservation-form" onSubmit={handleSubmit}>
                            <div className="modal-body">
                                <div className="row g-3">
                                    <div className="col-md-6 col-12">
                                        <label className="form-label" htmlFor="new-room">Room</label>
                                        <select className="form-select" id="new-room" required value={form.roomId} onChange={(e) => updateForm("roomId", e.target.value)}>
                                            {bookableRooms.map((room) => <option key={room.id} value={room.id}>{room.name} ({room.code})</option>)}
                                        </select>
                                        {selectedRoom && <div className="form-text">Up to {selectedRoom.capacity} people · {selectedRoom.floor}</div>}
                                    </div>
                                    <div className="col-md-6 col-12">
                                        <label className="form-label" htmlFor="new-organizer">Organizer</label>
                                        <select className="form-select" id="new-organizer" required value={form.organizerId} onChange={(e) => updateForm("organizerId", e.target.value)}>
                                            {allUsers.map((user) => <option key={user.id} value={user.id}>{user.fullName}</option>)}
                                        </select>
                                    </div>
                                    <div className="col-12">
                                        <label className="form-label" htmlFor="new-title">Meeting Title</label>
                                        <input type="text" className="form-control" id="new-title" placeholder="Enter meeting title" required maxLength="100"
                                               value={form.title} onChange={(e) => updateForm("title", e.target.value)} />
                                    </div>
                                    <div className="col-md-6 col-12">
                                        <label className="form-label" htmlFor="new-start">Start</label>
                                        <input type="datetime-local" className="form-control" id="new-start" required
                                               value={form.start} onChange={(e) => updateForm("start", e.target.value)} />
                                    </div>
                                    <div className="col-md-6 col-12">
                                        <label className="form-label" htmlFor="new-end">End</label>
                                        <input type="datetime-local" className="form-control" id="new-end" required
                                               value={form.end} onChange={(e) => updateForm("end", e.target.value)} />
                                    </div>
                                    <div className="col-md-6 col-12">
                                        <label className="form-label" htmlFor="new-attendees">Attendees</label>
                                        <input type="number" className="form-control" id="new-attendees" min="1" required
                                               value={form.attendees} onChange={(e) => updateForm("attendees", e.target.value)} />
                                    </div>
                                </div>
                                {rules.data && (
                                    <p className="form-text mt-3 mb-0">
                                        A reservation can last up to {rules.data.maxDurationDays} days and start at most {rules.data.bookingHorizonDays} days ahead.
                                    </p>
                                )}
                                <ErrorMessage error={formError} className="mt-3 mb-0" />
                            </div>
                            <div className="modal-footer">
                                <button type="button" className="btn btn-outline-gold" data-bs-dismiss="modal">Cancel</button>
                                <button type="submit" className="btn btn-gold" disabled={saving}>
                                    {saving ? "Saving..." : form.id ? "Save Changes" : "Save Reservation"}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </>
    );
}
