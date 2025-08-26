import React from "react";
import { Link } from "react-router-dom";

const Home = () => {
  return (
    <div className="container">
      <section style={{ padding: "2rem 0", textAlign: "center" }}>
        <h1 style={{ fontSize: "2.5rem", marginBottom: "1rem" }}>Welcome to Amaara Creations</h1>
        <p style={{ fontSize: "1.1rem", color: "#555" }}>
          We print custom stickers for weddings, cars, and more!
        </p>
        <div style={{ marginTop: "2rem" }}>
          <Link to="/products" className="home-btn">
            Browse Stickers
          </Link>
        </div>
      </section>
    </div>
  );
};

export default Home;
