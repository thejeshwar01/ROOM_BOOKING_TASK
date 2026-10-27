import { useEffect, useState } from "react";
import { api } from "../api.js";
import { useApi } from "../useApi.js";
import { endOfDay, formatDate, startOfDay } from "../dates.js";
import ErrorMessage from "../components/ErrorMessage.jsx";

// ///ROOM USAGE PAGE///
// Shows booked vs. actually used hours per room; all numbers are calculated by the back end
export default function Usage() {
    const [from, setFrom] = useState("");
    const [to, setTo] = useState("");

    useEffect(() => { document.title = "ROOM USAGE"; }, []);

    // Without dates the back end uses its default period (last 30 days)
    const report = useApi(() => api.getRoomUsage({
        from: from && startOfDay(from),
        to: to && endOfDay(to)
    }), [from, to]);

    const data = report.data;
    const underused = data ? data.rooms.filter((room) => room.isUnderused) : [];

    return (
        <section id="usage" className="p-lg-4 p-3">
            <div className="container-fluid">
                <div className="d-flex flex-wrap justify-content-between align-items-start gap-3">
                    <div>
                        <h1 className="page-title">Room Usage</h1>
                        <p className="page-subtitle">
                            Booked time compared with time actually used, to find rooms that are often booked but rarely used.
                        </p>
                    </div>
                    <button className="btn btn-outline-gold no-print" onClick={() => window.print()} disabled={!data}>
                        <i className="bi bi-file-earmark-pdf"></i> Export PDF
                    </button>
                </div>

                {/* Period */}
                <div className="row g-3 mb-4 no-print">
                    <div className="col-lg-3 col-md-4 col-6">
                        <div className="filter-box">
                            <i className="bi bi-calendar3"></i>
                            <div className="flex-grow-1">
                                <label htmlFor="usage-from">From</label>
                                <input type="date" className="form-control" id="usage-from" value={from} max={to || undefined}
                                       onChange={(e) => setFrom(e.target.value)} />
                            </div>
                        </div>
                    </div>
                    <div className="col-lg-3 col-md-4 col-6">
                        <div className="filter-box">
                            <i className="bi bi-calendar3"></i>
                            <div className="flex-grow-1">
                                <label htmlFor="usage-to">To</label>
                                <input type="date" className="form-control" id="usage-to" value={to} min={from || undefined}
                                       onChange={(e) => setTo(e.target.value)} />
                            </div>
                        </div>
                    </div>
                    <div className="col-lg-2 col-md-4 col-12">
                        <button className="btn btn-outline-gold w-100 h-100" onClick={() => { setFrom(""); setTo(""); }}>Last 30 Days</button>
                    </div>
                </div>

                <ErrorMessage error={report.error} className="mb-3" />

                {data && (
                    <>
                        <p className="usage-period">
                            {formatDate(data.from)} – {formatDate(data.to)} · Rooms using less than {data.lowUtilizationPercent}% of their booked time are flagged.
                        </p>
                        <p className={"usage-summary " + (underused.length ? "has-underused" : "")}>
                            <i className={"bi " + (underused.length ? "bi-exclamation-circle" : "bi-check-circle")}></i>{" "}
                            {underused.length
                                ? `Underused: ${underused.map((room) => room.roomName).join(", ")}`
                                : "No underused rooms in this period."}
                        </p>
                    </>
                )}

                {/* Usage table */}
                <div className="table-panel">
                    <div className="table-responsive">
                        <table className="table align-middle mb-0" id="usage-table">
                            <thead>
                                <tr>
                                    <th>Room</th>
                                    <th className="text-center">Reservations</th>
                                    <th className="text-center">Cancelled</th>
                                    <th className="text-center">Ended Early</th>
                                    <th className="text-center">Booked Hours</th>
                                    <th className="text-center">Used Hours</th>
                                    <th>Utilization</th>
                                </tr>
                            </thead>
                            <tbody>
                                {(data?.rooms ?? []).map((room) => (
                                    <tr key={room.roomId}>
                                        <td>
                                            <span className="room-name">{room.roomName}</span>
                                            <small className="room-id d-block">{room.roomCode}{room.isActive ? "" : " · Inactive"}</small>
                                        </td>
                                        <td className="text-center">{room.totalReservations}</td>
                                        <td className="text-center">{room.cancelledReservations} <small className="text-secondary">({room.cancellationPercent}%)</small></td>
                                        <td className="text-center">{room.endedEarlyReservations}</td>
                                        <td className="text-center">{room.bookedHours}</td>
                                        <td className="text-center">{room.usedHours}</td>
                                        <td className="usage-cell">
                                            {room.utilizationPercent === null ? (
                                                <span className="text-secondary">No completed meetings</span>
                                            ) : (
                                                <div className="d-flex align-items-center gap-2">
                                                    <div className="usage-bar">
                                                        <div className={"usage-fill" + (room.isUnderused ? " low" : "")}
                                                             style={{ width: Math.min(room.utilizationPercent, 100) + "%" }}></div>
                                                    </div>
                                                    <span className="usage-value">{room.utilizationPercent}%</span>
                                                    {room.isUnderused && <span className="status-badge status-cancelled">Underused</span>}
                                                </div>
                                            )}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                    {report.loading && !data && <p className="empty-message text-center">Loading report...</p>}
                </div>
            </div>
        </section>
    );
}
