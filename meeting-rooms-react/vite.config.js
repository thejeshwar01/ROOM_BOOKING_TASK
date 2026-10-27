import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
    plugins: [react()],
    server: {
        // Forward API calls to the ASP.NET back end (MeetingRooms.Api, "http" profile).
        // Set API_URL to use another address.
        proxy: {
            "/api": process.env.API_URL ?? "http://localhost:5228"
        }
    }
});
