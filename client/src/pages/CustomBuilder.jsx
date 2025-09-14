import React, { useState } from "react";
import './CustomBuilder.css';

const CustomBuilder = () => {
  const [text, setText] = useState("");
  const [font, setFont] = useState("Arial");
  const [width, setWidth] = useState(10); // in cm
  const [height, setHeight] = useState(5); // in cm
  const [isAddingToCart, setIsAddingToCart] = useState(false);
  const ratePerSqCm = 2; // Example rate

  const area = width * height;
  const cost = area * ratePerSqCm;

  const handleAddToCart = () => {
    if (!text.trim()) {
      alert("Please enter some text for your sticker");
      return;
    }
    
    setIsAddingToCart(true);
    // Simulate API call
    setTimeout(() => {
      alert(`Added to cart: ${text} (${width}cm x ${height}cm) - Rs. ${cost}`);
      setIsAddingToCart(false);
    }, 1000);
  };

  return (
    <div className="custom-builder-container">
      <div className="page-header">
        <h2 className="page-title">Custom Sticker Builder</h2>
        <p className="page-subtitle">Create your personalized stickers with our easy-to-use builder</p>
      </div>

      <div className="builder-content">
        <div className="builder-form">
          <div className="form-group">
            <label className="form-label">Enter Text</label>
            <input
              type="text"
              value={text}
              onChange={(e) => setText(e.target.value)}
              placeholder="Your text here..."
              className="form-input"
              maxLength={50}
            />
            <div className="character-count">{text.length}/50</div>
          </div>

          <div className="form-group">
            <label className="form-label">Select Font</label>
            <div className="select-wrapper">
              <select
                value={font}
                onChange={(e) => setFont(e.target.value)}
                className="form-select"
              >
                <option value="Arial">Arial</option>
                <option value="'Playfair Display', serif">Playfair Display</option>
                <option value="'Montserrat', sans-serif">Montserrat</option>
                <option value="'Dancing Script', cursive">Dancing Script</option>
                <option value="'Poppins', sans-serif">Poppins</option>
                <option value="'Great Vibes', cursive">Great Vibes</option>
              </select>
              <span className="select-arrow">▼</span>
            </div>
          </div>

          <div className="dimensions-container">
            <div className="dimensions-row">
              <div className="dimension-group">
                <label className="dimension-label">Width (cm)</label>
                <div className="dimension-controls">
                  <button 
                    className="dimension-btn decrease"
                    onClick={() => setWidth(prev => Math.max(1, prev - 1))}
                    disabled={width <= 1}
                    aria-label="Decrease width"
                  >
                    <svg width="12" height="2" viewBox="0 0 12 2" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M1 1H11" stroke="currentColor" strokeWidth="2" strokeLinecap="round"/>
                    </svg>
                  </button>
                  
                  <div className="dimension-value">
                    <input
                      type="number"
                      value={width}
                      onChange={(e) => setWidth(Math.max(1, Math.min(100, Number(e.target.value) || 1)))}
                      min="1"
                      max="100"
                      className="dimension-input"
                      aria-label="Width in centimeters"
                    />
                  </div>
                  
                  <button 
                    className="dimension-btn increase"
                    onClick={() => setWidth(prev => Math.min(100, prev + 1))}
                    disabled={width >= 100}
                    aria-label="Increase width"
                  >
                    <svg width="12" height="12" viewBox="0 0 12 12" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M6 1V11M1 6H11" stroke="currentColor" strokeWidth="2" strokeLinecap="round"/>
                    </svg>
                  </button>
                </div>
              </div>

              <div className="dimension-separator">×</div>

              <div className="dimension-group">
                <label className="dimension-label">Height (cm)</label>
                <div className="dimension-controls">
                  <button 
                    className="dimension-btn decrease"
                    onClick={() => setHeight(prev => Math.max(1, prev - 1))}
                    disabled={height <= 1}
                    aria-label="Decrease height"
                  >
                    <svg width="12" height="2" viewBox="0 0 12 2" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M1 1H11" stroke="currentColor" strokeWidth="2" strokeLinecap="round"/>
                    </svg>
                  </button>
                  
                  <div className="dimension-value">
                    <input
                      type="number"
                      value={height}
                      onChange={(e) => setHeight(Math.max(1, Math.min(100, Number(e.target.value) || 1)))}
                      min="1"
                      max="100"
                      className="dimension-input"
                      aria-label="Height in centimeters"
                    />
                  </div>
                  
                  <button 
                    className="dimension-btn increase"
                    onClick={() => setHeight(prev => Math.min(100, prev + 1))}
                    disabled={height >= 100}
                    aria-label="Increase height"
                  >
                    <svg width="12" height="12" viewBox="0 0 12 12" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M6 1V11M1 6H11" stroke="currentColor" strokeWidth="2" strokeLinecap="round"/>
                    </svg>
                  </button>
                </div>
              </div>
            </div>
            
            <div className="dimension-summary">
              <span className="summary-label">Total Area:</span>
              <span className="summary-value">{width * height} cm²</span>
            </div>
          </div>

          <div className="price-summary">
            <div className="price-detail">
              <span>Size:</span>
              <span>{width}cm × {height}cm</span>
            </div>
            <div className="price-detail">
              <span>Area:</span>
              <span>{area} cm²</span>
            </div>
            <div className="price-total">
              <span>Total:</span>
              <span>Rs. {cost}</span>
            </div>
          </div>

          <button 
            className={`add-to-cart-btn ${isAddingToCart ? 'loading' : ''}`}
            onClick={handleAddToCart}
            disabled={isAddingToCart || !text.trim()}
          >
            {isAddingToCart ? 'Adding to Cart...' : 'Add to Cart'}
          </button>
        </div>

        <div className="preview-container">
          <h3 className="preview-title">Live Preview</h3>
          <div 
            className="sticker-preview"
            style={{
              fontFamily: font,
              fontSize: `${Math.min(100, Math.max(16, 100 - text.length))}px`,
              lineHeight: '1.2',
              whiteSpace: 'pre-wrap',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              maxHeight: '100%',
              width: '100%',
              padding: '1rem',
              boxSizing: 'border-box',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              wordBreak: 'break-word',
              textAlign: 'center'
            }}
          >
            {text || "Your sticker preview"}
          </div>
        </div>
      </div>
    </div>
  );
};

export default CustomBuilder;
