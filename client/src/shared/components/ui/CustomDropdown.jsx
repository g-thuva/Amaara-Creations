import { useEffect, useId, useMemo, useRef, useState } from 'react';
import Icon from '@/shared/components/ui/Icon.jsx';

/**
 * Custom luxury dropdown select replacing native OS select popups.
 * Positions the dropdown menu right beneath the trigger, with custom
 * borders, radius, smooth hover states, and checkmark indicators.
 */
export function CustomDropdown({
  id,
  name,
  value: controlledValue,
  defaultValue = '',
  onChange,
  options = [],
  placeholder = 'Select...',
  ariaLabel,
  className = '',
  triggerClassName = '',
  align = 'left',
}) {
  const generatedId = useId();
  const dropdownId = id || generatedId;
  const containerRef = useRef(null);

  const [internalValue, setInternalValue] = useState(defaultValue);
  const [isOpen, setIsOpen] = useState(false);
  const [highlightedIndex, setHighlightedIndex] = useState(-1);

  const isControlled = controlledValue !== undefined;
  const currentValue = isControlled ? controlledValue : internalValue;

  // Normalise options of shape [[val, label]], [{ value, label }], or [{ id, name }]
  const normalizedOptions = useMemo(() => {
    return options.map((opt) => {
      if (Array.isArray(opt)) {
        return { value: String(opt[0]), label: String(opt[1]) };
      }
      if (typeof opt === 'object' && opt !== null) {
        const val = opt.value !== undefined ? opt.value : opt.id !== undefined ? opt.id : '';
        const lbl = opt.label !== undefined ? opt.label : opt.name !== undefined ? opt.name : String(val);
        return { value: String(val), label: String(lbl) };
      }
      return { value: String(opt), label: String(opt) };
    });
  }, [options]);

  const selectedOption = normalizedOptions.find(
    (opt) => String(opt.value) === String(currentValue),
  ) || normalizedOptions[0];

  // Close when clicking outside or pressing Escape
  useEffect(() => {
    if (!isOpen) return undefined;

    const handleClickOutside = (e) => {
      if (containerRef.current && !containerRef.current.contains(e.target)) {
        setIsOpen(false);
      }
    };

    const handleKeyDown = (e) => {
      if (e.key === 'Escape') {
        setIsOpen(false);
      } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        setHighlightedIndex((prev) =>
          prev < normalizedOptions.length - 1 ? prev + 1 : 0,
        );
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        setHighlightedIndex((prev) =>
          prev > 0 ? prev - 1 : normalizedOptions.length - 1,
        );
      } else if (e.key === 'Enter' && isOpen && highlightedIndex >= 0) {
        e.preventDefault();
        const item = normalizedOptions[highlightedIndex];
        if (item) {
          handleSelect(item.value);
        }
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    document.addEventListener('touchstart', handleClickOutside);
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
      document.removeEventListener('touchstart', handleClickOutside);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, highlightedIndex, normalizedOptions]);

  const handleSelect = (optionValue) => {
    if (!isControlled) {
      setInternalValue(optionValue);
    }
    onChange?.(optionValue);
    setIsOpen(false);
  };

  const toggleDropdown = () => {
    setIsOpen((prev) => {
      if (!prev) {
        const curIdx = normalizedOptions.findIndex(
          (opt) => String(opt.value) === String(currentValue),
        );
        setHighlightedIndex(curIdx >= 0 ? curIdx : 0);
      }
      return !prev;
    });
  };

  return (
    <div
      ref={containerRef}
      className={`s-custom-dropdown ${isOpen ? 'is-open' : ''} ${className}`.trim()}
    >
      {/* Hidden input ensures standard HTML form submission via new FormData(form) continues to work */}
      {name && <input type="hidden" name={name} value={currentValue} />}

      <button
        type="button"
        id={dropdownId}
        className={`s-dropdown-trigger ${triggerClassName}`.trim()}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-label={ariaLabel || selectedOption?.label || placeholder}
        onClick={toggleDropdown}
      >
        <span className="s-dropdown-value">
          {selectedOption?.label || placeholder}
        </span>
        <Icon
          name="chevronDown"
          className={`s-dropdown-chevron ${isOpen ? 'is-open' : ''}`}
          aria-hidden="true"
        />
      </button>

      {isOpen && (
        <ul
          className={`s-dropdown-menu align-${align}`}
          role="listbox"
          aria-labelledby={dropdownId}
        >
          {normalizedOptions.map((opt, index) => {
            const isSelected = String(opt.value) === String(currentValue);
            const isHighlighted = index === highlightedIndex;

            return (
              <li
                key={opt.value}
                role="option"
                aria-selected={isSelected}
                className={`s-dropdown-item ${isSelected ? 'is-selected' : ''} ${isHighlighted ? 'is-highlighted' : ''}`}
                onClick={() => handleSelect(opt.value)}
                onMouseEnter={() => setHighlightedIndex(index)}
              >
                <span className="s-dropdown-item-label">{opt.label}</span>
                {isSelected && (
                  <Icon name="check" className="s-dropdown-check" aria-hidden="true" />
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}

export default CustomDropdown;

