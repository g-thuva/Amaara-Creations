import React, { useState } from "react";

const CustomBuilder = () => {
  const [text, setText] = useState("");
  const [font, setFont] = useState("Arial");
  const [width, setWidth] = useState(10); // in cm
  const [height, setHeight] = useState(5); // in cm
  const ratePerSqCm = 2; // Example rate

  const area = width * height;
  const cost = area * ratePerSqCm;

  return (
    <div className="container" style={{ padding: "2rem 0" }}>
      <h2 style={{ fontSize: "2rem", marginBottom: "1.5rem" }}>Custom Sticker Builder</h2>

      <div className="builder-form" style={{ display: "flex", gap: "2rem", flexWrap: "wrap" }}>
        {/* Form */}
        <div style={{ flex: 1, minWidth: "300px" }}>
          <label>Enter Text:</label>
          <input
            type="text"
            value={text}
            onChange={(e) => setText(e.target.value)}
            placeholder="Your text here..."
            style={{ width: "100%", padding: "8px", margin: "10px 0" }}
          />

          <label>Select Font:</label>
          <select
            value={font}
            onChange={(e) => setFont(e.target.value)}
            style={{ width: "100%", padding: "8px", marginBottom: "10px" }}
          >
            <option value="Arial">Arial</option>
            <option value="Courier New">Courier New</option>
            <option value="Georgia">Georgia</option>
            <option value="Impact">Impact</option>
            <option value="Times New Roman">Times New Roman</option>
          </select>

          <div style={{ display: "flex", gap: "1rem", marginBottom: "10px" }}>
            <div>
              <label>Width (cm):</label>
              <input
                type="number"
                value={width}
                onChange={(e) => setWidth(Number(e.target.value))}
                style={{ width: "80px", padding: "6px" }}
              />
            </div>
            <div>
              <label>Height (cm):</label>
              <input
                type="number"
                value={height}
                onChange={(e) => setHeight(Number(e.target.value))}
                style={{ width: "80px", padding: "6px" }}
              />
            </div>
          </div>

          <p style={{ marginTop: "10px" }}>Estimated Cost: <strong>Rs. {cost}</strong></p>
          <button className="home-btn" style={{ marginTop: "1rem" }}>Add to Cart</button>
        </div>

        {/* Preview */}
        <div
          className="sticker-preview"
          style={{
            flex: 1,
            minWidth: "300px",
            border: "2px dashed #ccc",
            padding: "1rem",
            textAlign: "center",
            height: "200px",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            fontFamily: font,
            fontSize: "1.5rem",
            backgroundColor: "#fff"
          }}
        >
          {text || "Your sticker preview"}
        </div>
      </div>
    </div>
  );
};

export default CustomBuilder;
