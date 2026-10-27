import { useEffect, useRef } from "react";
import { Link, NavLink, useLocation } from "react-router-dom";
import { Collapse } from "bootstrap";

// ///SIDEBAR///
export default function Sidebar({ onHide }) {
    const menuRef = useRef(null);
    const location = useLocation();

    // Close the mobile menu after moving to another page
    useEffect(() => {
        const menu = Collapse.getInstance(menuRef.current);
        if (menu) menu.hide();
    }, [location.pathname]);

    return (
        <header className="sticky-top" id="sidebar">
            <nav className="navbar navbar-expand-lg" id="navbar">
                <div className="container-fluid">
                    <Link className="navbar-brand brand-icon" to="/" aria-label="Meeting Rooms home">
                        <i className="bi bi-calendar2-week"></i>
                    </Link>
                    <button className="sidebar-close" type="button" id="sidebar-close" aria-label="Hide navigation" title="Hide navigation"
                            onClick={onHide}>
                        <i className="bi bi-x-lg"></i>
                    </button>
                    <button className="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                        <i className="bi bi-list"></i>
                    </button>
                    <div className="collapse navbar-collapse" id="navbarSupportedContent" ref={menuRef}>
                        <ul className="navbar-nav">
                            <li className="nav-item">
                                <NavLink className="nav-link" to="/" end><i className="bi bi-door-open"></i> Rooms</NavLink>
                            </li>
                            <li className="nav-item">
                                <NavLink className="nav-link" to="/reservations"><i className="bi bi-calendar-check"></i> Reservations</NavLink>
                            </li>
                            <li className="nav-item">
                                <NavLink className="nav-link" to="/users"><i className="bi bi-people"></i> Users</NavLink>
                            </li>
                            <li className="nav-item">
                                <NavLink className="nav-link" to="/usage"><i className="bi bi-graph-up"></i> Usage</NavLink>
                            </li>
                        </ul>
                    </div>
                </div>
            </nav>
        </header>
    );
}
