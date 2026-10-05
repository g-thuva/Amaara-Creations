import { useState } from 'react';
import { Link } from 'react-router-dom';
import { PageHeader } from '../components/storefront/UI';
export default function CustomBuilder() {
  const [text, setText] = useState('Make it yours.');
  const [font, setFont] = useState('sans-serif');
  const [width, setWidth] = useState(10);
  const [height, setHeight] = useState(5);
  return <div className="s-container s-page"><PageHeader eyebrow="A space for your ideas" title="Small sticker. Big personality.">Explore a text sticker idea with our interactive preview.</PageHeader><div className="s-builder"><form className="s-form" onSubmit={e => e.preventDefault()}><label>Your words<input value={text} maxLength="50" onChange={e => setText(e.target.value)} placeholder="Something only you would say"/></label><p className="s-meta">{text.length}/50 characters</p><label>Type style<select value={font} onChange={e => setFont(e.target.value)}><option value="sans-serif">Clean sans serif</option><option value="serif">Classic serif</option><option value="monospace">Typewriter</option><option value="cursive">Handwritten</option></select></label><label>Width: {width} cm<input type="range" min="2" max="30" value={width} onChange={e => setWidth(Number(e.target.value))}/></label><label>Height: {height} cm<input type="range" min="2" max="30" value={height} onChange={e => setHeight(Number(e.target.value))}/></label><p className="s-alert">Preview only. Custom designs cannot be saved or ordered online yet. Your changes stay on this page.</p><Link className="s-button s-button-primary" to="/contact">Discuss your idea →</Link></form><div className="s-preview-area"><span className="s-eyebrow">Your sticker preview</span><div className="s-sticker-preview" style={{ fontFamily: font, aspectRatio: `${width} / ${height}` }}>{text || 'Your words here'}</div><p>{width} × {height} cm · on-screen proportions are illustrative</p></div></div></div>;
}
