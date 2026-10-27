import { useEffect, useState } from "react";
import { Routes, Route } from "react-router-dom";

import Sidebar from "./components/Sidebar.jsx";
import Footer from "./components/Footer.jsx";
import Rooms from "./pages/Rooms.jsx";
import Reservations from "./pages/Reservations.jsx";
import Users from "./pages/Users.jsx";
import Usage from "./pages/Usage.jsx";

// Hide/show the navbar and remember the choice across pages
function loadSidebarHidden() {
    try { return localStorage.getItem("sidebarHidden") === "1"; } catch (e) { return false; }
}

export default function App() {
    const [sidebarHidden, setSidebarHidden] = useState(loadSidebarHidden);

    useEffect(() => {
        document.body.classList.toggle("sidebar-hidden", sidebarHidden);
        try { localStorage.setItem("sidebarHidden", sidebarHidden ? "1" : "0"); } catch (e) {}
    }, [sidebarHidden]);

    return (
        <>
            <Sidebar onHide={() => setSidebarHidden(true)} />
            <button className="sidebar-open" type="button" id="sidebar-open" aria-label="Show navigation" title="Show navigation"
                    onClick={() => setSidebarHidden(false)}>
                <i className="bi bi-list"></i>
            </button>

            <main>
                <Routes>
                    <Route path="/" element={<Rooms />} />
                    <Route path="/reservations" element={<Reservations />} />
                    <Route path="/users" element={<Users />} />
                    <Route path="/usage" element={<Usage />} />
                    <Route path="*" element={<Rooms />} />
                </Routes>
            </main>

            <Footer />
        </>
    );
}
