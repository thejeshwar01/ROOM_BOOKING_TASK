import { useEffect, useState } from "react";
import { api } from "../api.js";
import { useApi } from "../useApi.js";
import ErrorMessage from "../components/ErrorMessage.jsx";

// ///USERS PAGE///
export default function Users() {
    const [search, setSearch] = useState("");
    const [sortOrder, setSortOrder] = useState(null);

    useEffect(() => { document.title = "USERS"; }, []);

    // Search and the active reservation counts come from the back end
    const users = useApi(() => api.getUsers(search.trim()), [search]);

    let visibleUsers = users.data ?? [];

    // Sort by active reservations (click again to reverse)
    if (sortOrder) {
        visibleUsers = [...visibleUsers].sort((a, b) => {
            const diff = a.activeReservations - b.activeReservations;
            return sortOrder === "desc" ? -diff : diff;
        });
    }

    function toggleSort() {
        setSortOrder(sortOrder === "desc" ? "asc" : "desc");
    }

    return (
        <section id="users" className="p-lg-4 p-3">
            <div className="container-fluid">
                <h1 className="page-title">Users</h1>
                <p className="page-subtitle">Manage users and view their active reservations.</p>

                <div className="row mb-4">
                    <div className="col-lg-5 col-md-7 col-12">
                        <div className="search-box">
                            <i className="bi bi-search"></i>
                            <input type="text" className="form-control" id="user-search" placeholder="Search by name or ID..."
                                   value={search} onChange={(e) => setSearch(e.target.value)} />
                        </div>
                    </div>
                </div>

                <ErrorMessage error={users.error} className="mb-3" />

                {/* User table */}
                <div className="table-panel">
                    <div className="table-responsive">
                        <table className="table align-middle mb-0" id="user-table">
                            <thead>
                                <tr>
                                    <th></th>
                                    <th>ID</th>
                                    <th>First Name</th>
                                    <th>Last Name</th>
                                    <th className="text-center sortable" id="sort-reservations" onClick={toggleSort}>Active Reservations <i className="bi bi-arrow-down-up"></i></th>
                                </tr>
                            </thead>
                            <tbody>
                                {visibleUsers.map((user) => (
                                    <tr key={user.id}>
                                        <td><span className="avatar">{user.firstName[0]}</span></td>
                                        <td>{user.code}</td>
                                        <td>{user.firstName}</td>
                                        <td>{user.lastName}</td>
                                        <td className="text-center gold-text">{user.activeReservations}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                    {users.loading && !users.data && <p className="empty-message text-center">Loading users...</p>}
                    {users.data && visibleUsers.length === 0 && <p className="empty-message text-center" id="no-users">No users found.</p>}
                </div>
            </div>
        </section>
    );
}
